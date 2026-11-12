using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace win_mouse_macro.models {
    // Keeps played-back input from looking mechanical: no two delays, positions or cursor paths repeat.
    public static class Humanizer {
        // Delays are scaled by a log-normal factor: usually close to 1, sometimes clearly slower.
        private const double DelaySpread = 0.15;
        private const double MinDelayFactor = 0.7;
        private const double MaxDelayFactor = 1.5;

        // Now and then a delay gets an extra pause, like a person hesitating.
        private const double HesitationChance = 0.04;
        private const int HesitationMinMs = 250;
        private const int HesitationMaxMs = 1200;

        // How long a button or key stays down, and the short pauses around a press.
        private const int HoldMinMs = 45;
        private const int HoldMaxMs = 130;

        // A recorded long hold is replayed a little longer, never shorter: releasing early could change what it does.
        private const double LongHoldSpread = 0.05;
        private const double MaxLongHoldExtra = 0.2;

        private const double PositionSpreadPx = 1.5;
        private const int MaxPositionOffsetPx = 4;

        // Waits this long or longer nudge the cursor around instead of leaving it perfectly still.
        private const int LongWaitMs = 1000;
        private const int DriftPauseMinMs = 150;
        private const int DriftPauseMaxMs = 700;
        private const int DriftStepPx = 2;
        private const int DriftRadiusPx = 6;

        private const int MoveStepMs = 10;
        private const double MoveBaseMs = 120;
        private const double MoveMsPerDistanceDoubling = 55;
        private const double MoveDistanceScalePx = 15;
        // How far the cursor path may bend sideways, as a fraction of the distance travelled.
        private const double MaxPathBow = 0.12;

        // The one generator behind all randomness. Random is not thread-safe; playback only runs on the UI thread.
        private static readonly Random _random = new Random();

        // Waits for roughly baseMs. Returns early, without throwing, once cancelled.
        public static async Task DelayAsync(int baseMs, CancellationToken cancellation) {
            int delayMs = JitterDelay(baseMs);
            if (delayMs < LongWaitMs) {
                await WaitAsync(delayMs, cancellation);
            } else {
                await DriftAsync(delayMs, cancellation);
            }
        }

        public static Task HoldAsync() {
            return Task.Delay(_random.Next(HoldMinMs, HoldMaxMs + 1));
        }

        // Waits about as long as a button was held while recording. Returns early, without throwing, once cancelled.
        // The cursor is left alone: moving it with the button down would turn the hold into a drag.
        public static Task LongHoldAsync(int recordedMs, CancellationToken cancellation) {
            return WaitAsync(LongHoldMs(recordedMs), cancellation);
        }

        public static (int X, int Y) NextPositionOffset() {
            return (NextPositionOffsetOnAxis(), NextPositionOffsetOnAxis());
        }

        // Moves the cursor to the target along a curved, eased path instead of teleporting it there.
        public static async Task MoveCursorAsync(int targetX, int targetY) {
            if (MouseOperations.TryGetPosition(out int startX, out int startY)) {
                double deltaX = targetX - startX;
                double deltaY = targetY - startY;
                double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (distance >= 1) {
                    // Farther targets take longer, but less than proportionally (Fitts's law).
                    double travelMs = NextDelayFactor()
                        * (MoveBaseMs + MoveMsPerDistanceDoubling * Math.Log(1 + distance / MoveDistanceScalePx, 2));

                    // Bend the path through a control point pushed sideways off the straight line.
                    double bow = distance * MaxPathBow * (_random.NextDouble() * 2 - 1);
                    double controlX = startX + deltaX / 2 - deltaY / distance * bow;
                    double controlY = startY + deltaY / 2 + deltaX / distance * bow;

                    Stopwatch stopwatch = Stopwatch.StartNew();
                    while (true) {
                        await Task.Delay(MoveStepMs);
                        double progress = stopwatch.ElapsedMilliseconds / travelMs;
                        if (progress >= 1) break;

                        // Ease in and out: slow start, fast middle, slow arrival.
                        double t = progress * progress * (3 - 2 * progress);
                        double x = (1 - t) * (1 - t) * startX + 2 * (1 - t) * t * controlX + t * t * targetX;
                        double y = (1 - t) * (1 - t) * startY + 2 * (1 - t) * t * controlY + t * t * targetY;
                        MouseOperations.MoveTo((int)Math.Round(x), (int)Math.Round(y));
                    }
                }
            }

            MouseOperations.MoveTo(targetX, targetY);
        }

        private static async Task DriftAsync(int durationMs, CancellationToken cancellation) {
            Stopwatch stopwatch = Stopwatch.StartNew();
            MouseOperations.TryGetPosition(out int anchorX, out int anchorY);
            int lastX = anchorX;
            int lastY = anchorY;

            while (true) {
                long remainingMs = durationMs - stopwatch.ElapsedMilliseconds;
                int pauseMs = _random.Next(DriftPauseMinMs, DriftPauseMaxMs + 1);
                if (remainingMs <= pauseMs) {
                    await WaitAsync((int)Math.Max(remainingMs, 0), cancellation);
                    return;
                }

                await WaitAsync(pauseMs, cancellation);
                if (cancellation.IsCancellationRequested) return;
                if (!MouseOperations.TryGetPosition(out int currentX, out int currentY)) continue;
                if (currentX != lastX || currentY != lastY) {
                    // The user moved the mouse: drift around the new spot instead of pulling the cursor back.
                    anchorX = currentX;
                    anchorY = currentY;
                }

                lastX = Clamp(currentX + _random.Next(-DriftStepPx, DriftStepPx + 1), anchorX - DriftRadiusPx, anchorX + DriftRadiusPx);
                lastY = Clamp(currentY + _random.Next(-DriftStepPx, DriftStepPx + 1), anchorY - DriftRadiusPx, anchorY + DriftRadiusPx);
                MouseOperations.MoveTo(lastX, lastY);
            }
        }

        // Task.WhenAny completes normally when the delay is cancelled, so stopping needs no exception handling.
        private static Task WaitAsync(int delayMs, CancellationToken cancellation) {
            return Task.WhenAny(Task.Delay(delayMs, cancellation));
        }

        private static int JitterDelay(int baseMs) {
            if (baseMs <= 0) return 0;

            double delayMs = baseMs * NextDelayFactor();
            if (_random.NextDouble() < HesitationChance) {
                delayMs += _random.Next(HesitationMinMs, HesitationMaxMs + 1);
            }
            return (int)Math.Min(delayMs, int.MaxValue);
        }

        private static int LongHoldMs(int recordedMs) {
            double extra = Math.Min(Math.Abs(NextGaussian()) * LongHoldSpread, MaxLongHoldExtra);
            return (int)Math.Min(recordedMs * (1 + extra), int.MaxValue);
        }

        private static double NextDelayFactor() {
            double factor = Math.Exp(NextGaussian() * DelaySpread);
            return Math.Max(MinDelayFactor, Math.Min(MaxDelayFactor, factor));
        }

        private static int NextPositionOffsetOnAxis() {
            return Clamp((int)Math.Round(NextGaussian() * PositionSpreadPx), -MaxPositionOffsetPx, MaxPositionOffsetPx);
        }

        // Box-Muller transform: a bell-curve value centred on 0 with a standard deviation of 1.
        private static double NextGaussian() {
            double u1 = 1.0 - _random.NextDouble();
            double u2 = _random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        private static int Clamp(int value, int min, int max) {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
