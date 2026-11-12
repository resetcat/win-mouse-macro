using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace win_mouse_macro.models {
    // Turns the raw button and key events from the hooks into macro records.
    public static class InputRecorder {
        private static readonly Dictionary<MouseButton, (ClickRecord Click, uint TimeMs)> _pendingPresses =
            new Dictionary<MouseButton, (ClickRecord Click, uint TimeMs)>();
        private static bool _modifierTapPending;

        public static void MouseDown(MouseButton button, int x, int y, uint timeMs) {
            // Clicking while a modifier is held uses it, so releasing it afterwards is not a tap of its own.
            _modifierTapPending = false;
            ClickRecord click = new ClickRecord {
                Button = button,
                X = x,
                Y = y,
                Modifiers = KeyboardOperations.GetHeldModifiers()
            };
            _pendingPresses[button] = (click, timeMs);
        }

        // Returns the finished click, hold or drag, or null when the press was never seen.
        public static MacroRecord MouseUp(MouseButton button, int x, int y, uint timeMs) {
            if (!_pendingPresses.TryGetValue(button, out var press)) return null;
            _pendingPresses.Remove(button);
            ClickRecord click = press.Click;

            bool isDrag = Math.Abs(x - click.X) > SystemParameters.MinimumHorizontalDragDistance
                || Math.Abs(y - click.Y) > SystemParameters.MinimumVerticalDragDistance;
            if (!isDrag) {
                // Event times are tick counts that wrap around; unsigned subtraction still gives the time between them.
                click.HoldMs = (int)Math.Min(unchecked(timeMs - press.TimeMs), int.MaxValue);
                return click;
            }

            return new DragRecord {
                Button = button,
                FromX = click.X,
                FromY = click.Y,
                ToX = x,
                ToY = y,
                Modifiers = click.Modifiers
            };
        }

        // Returns the key press to record, or null for modifiers, which are recorded with the key they modify.
        public static KeyRecord KeyDown(int virtualKey, int scanCode, bool isExtended) {
            if (KeyboardOperations.IsModifier(virtualKey)) {
                // A held modifier auto-repeats. Inside the hook the key state is still the one from before
                // this event, so "already down" means a repeat rather than a fresh press.
                if (!KeyboardOperations.IsKeyDown(virtualKey)) _modifierTapPending = true;
                return null;
            }

            _modifierTapPending = false;
            return new KeyRecord {
                VirtualKey = virtualKey,
                ScanCode = scanCode,
                IsExtended = isExtended,
                Modifiers = KeyboardOperations.GetHeldModifiers()
            };
        }

        // Returns a record only for a modifier pressed and released on its own (Win, Alt, Ctrl+Shift...).
        public static KeyRecord KeyUp(int virtualKey, int scanCode, bool isExtended) {
            if (!_modifierTapPending || !KeyboardOperations.IsModifier(virtualKey)) return null;
            _modifierTapPending = false;

            // The released key still reads as down inside the hook, so drop it from its own modifiers.
            List<int> otherModifiers = KeyboardOperations.GetHeldModifiers();
            otherModifiers.Remove(virtualKey);
            return new KeyRecord {
                VirtualKey = virtualKey,
                ScanCode = scanCode,
                IsExtended = isExtended,
                Modifiers = otherModifiers
            };
        }
    }
}
