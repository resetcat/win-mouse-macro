using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace win_mouse_macro.models {
    public class MouseOperations {
        [Flags]
        public enum MouseEventFlags {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public static void MoveTo(int x, int y) {
            SetCursorPos(x, y);
        }

        // Fails when the cursor cannot be read, for example while the lock screen is showing.
        public static bool TryGetPosition(out int x, out int y) {
            bool succeeded = GetCursorPos(out POINT point);
            x = point.x;
            y = point.y;
            return succeeded;
        }

        public static void ButtonDown(MouseButton button) {
            SendButtonEvent(button == MouseButton.Right ? MouseEventFlags.RIGHTDOWN : MouseEventFlags.LEFTDOWN);
        }

        public static void ButtonUp(MouseButton button) {
            SendButtonEvent(button == MouseButton.Right ? MouseEventFlags.RIGHTUP : MouseEventFlags.LEFTUP);
        }

        // Button events act at the current cursor position.
        private static void SendButtonEvent(MouseEventFlags flags) {
            mouse_event((int)flags, 0, 0, 0, 0);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT {
            public int x;
            public int y;
        }
    }
}
