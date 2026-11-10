using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace win_mouse_macro.models {
    // Turns the raw button and key events from the hooks into macro records.
    public static class InputRecorder {
        private static readonly Dictionary<MouseButton, ClickRecord> _pendingClicks = new Dictionary<MouseButton, ClickRecord>();
        private static bool _modifierTapPending;

        public static void MouseDown(MouseButton button, int x, int y) {
            // Clicking while a modifier is held uses it, so releasing it afterwards is not a tap of its own.
            _modifierTapPending = false;
            _pendingClicks[button] = new ClickRecord {
                Button = button,
                X = x,
                Y = y,
                Modifiers = KeyboardOperations.GetHeldModifiers()
            };
        }

        // Returns the finished click or drag, or null when the press was never seen.
        public static MacroRecord MouseUp(MouseButton button, int x, int y) {
            if (!_pendingClicks.TryGetValue(button, out ClickRecord click)) return null;
            _pendingClicks.Remove(button);

            bool isDrag = Math.Abs(x - click.X) > SystemParameters.MinimumHorizontalDragDistance
                || Math.Abs(y - click.Y) > SystemParameters.MinimumVerticalDragDistance;
            if (!isDrag) return click;

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
