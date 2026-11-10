using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace win_mouse_macro.models {
    public class KeyboardHook {
        private const int WH_KEYBOARD_LL = 13;
        private const uint LLKHF_EXTENDED = 0x01;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static void SetHook() {
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        }

        public static void ReleaseHook() {
            UnhookWindowsHookEx(_hookID);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0) {
                KeyboardMessages message = (KeyboardMessages)wParam;
                bool isKeyDown = message == KeyboardMessages.WM_KEYDOWN || message == KeyboardMessages.WM_SYSKEYDOWN;
                bool isKeyUp = message == KeyboardMessages.WM_KEYUP || message == KeyboardMessages.WM_SYSKEYUP;
                if (isKeyDown || isKeyUp) {
                    KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    int virtualKey = (int)hookStruct.vkCode;
                    int scanCode = (int)hookStruct.scanCode;
                    bool isExtended = (hookStruct.flags & LLKHF_EXTENDED) != 0;
                    KeyRecord record = isKeyDown
                        ? InputRecorder.KeyDown(virtualKey, scanCode, isExtended)
                        : InputRecorder.KeyUp(virtualKey, scanCode, isExtended);

                    if (record != null) {
                        Application.Current.Dispatcher.Invoke(() => {
                            var mainWindow = (MainWindow)Application.Current.MainWindow;
                            // Keys typed into the macro window itself (F5/F6/F8, the delay box) are not part of the macro.
                            if (mainWindow.isRecording && !mainWindow.IsActive) {
                                mainWindow.AddRecord(record);
                            }
                        });
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private enum KeyboardMessages {
            WM_KEYDOWN = 0x0100,
            WM_KEYUP = 0x0101,
            WM_SYSKEYDOWN = 0x0104,
            WM_SYSKEYUP = 0x0105
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
