using System.Threading.Tasks;
using System.Windows.Input;

namespace win_mouse_macro.models {
    public class DragRecord : MacroRecord {
        public MouseButton Button { get; set; }
        public int FromX { get; set; }
        public int FromY { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }

        protected override async Task PlayActionAsync() {
            // Both ends share one offset so the drag keeps its recorded length and direction.
            var offset = Humanizer.NextPositionOffset();
            await Humanizer.MoveCursorAsync(FromX + offset.X, FromY + offset.Y);
            await Humanizer.HoldAsync();
            MouseOperations.ButtonDown(Button);
            await Humanizer.HoldAsync();
            await Humanizer.MoveCursorAsync(ToX + offset.X, ToY + offset.Y);
            await Humanizer.HoldAsync();
            MouseOperations.ButtonUp(Button);
        }

        public override string ToString() {
            return $"{ModifierPrefix}{Button} button dragged from ({FromX}, {FromY}) to ({ToX}, {ToY})";
        }
    }
}
