using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace win_mouse_macro.models {
    public class ClickRecord : MacroRecord {
        // A press at least this long was held on purpose and is replayed for its recorded length.
        // Anything shorter is an ordinary click.
        private const int MinHoldMs = 300;

        public MouseButton Button { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        // How long the button stayed down while recording.
        public int HoldMs { get; set; }

        public bool IsHold {
            get { return HoldMs >= MinHoldMs; }
        }

        protected override async Task PlayActionAsync(CancellationToken cancellation) {
            var offset = Humanizer.NextPositionOffset();
            await Humanizer.MoveCursorAsync(X + offset.X, Y + offset.Y);
            await Humanizer.HoldAsync();
            MouseOperations.ButtonDown(Button);
            if (IsHold) {
                await Humanizer.LongHoldAsync(HoldMs, cancellation);
            } else {
                await Humanizer.HoldAsync();
            }
            MouseOperations.ButtonUp(Button);
        }

        public override string ToString() {
            if (IsHold) {
                return $"{ModifierPrefix}{Button} button held at ({X}, {Y}) for {HoldMs / 1000.0:0.0} s";
            }
            return $"{ModifierPrefix}{Button} button clicked at ({X}, {Y})";
        }
    }
}
