using System.Threading;
using System.Threading.Tasks;

namespace win_mouse_macro.models {
    public class KeyRecord : MacroRecord {
        public int VirtualKey { get; set; }
        public int ScanCode { get; set; }
        // Tells apart keys that share a virtual-key code, such as the arrow keys and the numpad arrows.
        public bool IsExtended { get; set; }

        protected override async Task PlayActionAsync(CancellationToken cancellation) {
            KeyboardOperations.KeyDown(VirtualKey, ScanCode, IsExtended);
            await Humanizer.HoldAsync();
            KeyboardOperations.KeyUp(VirtualKey, ScanCode, IsExtended);
        }

        public override string ToString() {
            return $"Key pressed: {ModifierPrefix}{KeyboardOperations.GetKeyName(VirtualKey)}";
        }
    }
}
