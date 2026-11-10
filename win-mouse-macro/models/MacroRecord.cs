using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace win_mouse_macro.models {
    public abstract class MacroRecord {
        // Virtual-key codes of the Ctrl, Shift, Alt and Win keys held while the action happened.
        public List<int> Modifiers { get; set; } = new List<int>();

        public async Task PlayAsync() {
            int pressedCount = 0;
            try {
                foreach (int modifier in Modifiers) {
                    KeyboardOperations.ModifierDown(modifier);
                    pressedCount++;
                    await Humanizer.HoldAsync();
                }

                await PlayActionAsync();
            } finally {
                // A modifier left down would change everything the user does next, so always release.
                for (int i = pressedCount - 1; i >= 0; i--) {
                    await Humanizer.HoldAsync();
                    KeyboardOperations.ModifierUp(Modifiers[i]);
                }
            }
        }

        protected abstract Task PlayActionAsync();

        protected string ModifierPrefix {
            get { return string.Concat(Modifiers.Select(modifier => KeyboardOperations.GetKeyName(modifier) + "+")); }
        }
    }
}
