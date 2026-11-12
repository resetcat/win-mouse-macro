using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace win_mouse_macro.models {
    public class MouseHook {
        private const int WH_MOUSE_LL = 14;
        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static void SetHook() {
            _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(null), 0);
        }

        public static void ReleaseHook() {
            UnhookWindowsHookEx(_hookID);
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0) {
                MouseMessages message = (MouseMessages)wParam;
                bool isButtonDown = message == MouseMessages.WM_LBUTTONDOWN || message == MouseMessages.WM_RBUTTONDOWN;
                bool isButtonUp = message == MouseMessages.WM_LBUTTONUP || message == MouseMessages.WM_RBUTTONUP;
                if (isButtonDown || isButtonUp) {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    bool isLeftButton = message == MouseMessages.WM_LBUTTONDOWN || message == MouseMessages.WM_LBUTTONUP;
                    MouseButton button = isLeftButton ? MouseButton.Left : MouseButton.Right;

                    if (isButtonDown) {
                        InputRecorder.MouseDown(button, hookStruct.pt.x, hookStruct.pt.y, hookStruct.time);
                    } else {
                        // A click, hold or drag is only known once the button is released.
                        MacroRecord record = InputRecorder.MouseUp(button, hookStruct.pt.x, hookStruct.pt.y, hookStruct.time);
                        if (record != null) {
                            // Only log and update if recording is active
                            Application.Current.Dispatcher.Invoke(() => {
                                var mainWindow = (MainWindow)Application.Current.MainWindow;
                                if (mainWindow.isRecording) {
                                    mainWindow.AddRecord(record);
                                }
                            });
                        }
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }



        private enum MouseMessages {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT {
            public int x;
            public int y;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

}
