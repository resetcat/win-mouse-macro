using System.Threading.Tasks;
using System.Windows.Input;

namespace win_mouse_macro.models {
    public class ClickRecord : MacroRecord {
        public MouseButton Button { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        protected override async Task PlayActionAsync() {
            var offset = Humanizer.NextPositionOffset();
            await Humanizer.MoveCursorAsync(X + offset.X, Y + offset.Y);
            await Humanizer.HoldAsync();
            MouseOperations.ButtonDown(Button);
            await Humanizer.HoldAsync();
            MouseOperations.ButtonUp(Button);
        }

        public override string ToString() {
            return $"{ModifierPrefix}{Button} button clicked at ({X}, {Y})";
        }
    }
}
