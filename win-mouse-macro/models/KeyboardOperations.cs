using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace win_mouse_macro.models {
    public static class KeyboardOperations {
        private const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const uint MAPVK_VK_TO_VSC = 0;
        private const int KeyDownBit = 0x8000;

        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_LSHIFT = 0xA0;
        private const int VK_RSHIFT = 0xA1;
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_LMENU = 0xA4;
        private const int VK_RMENU = 0xA5;

        private static readonly int[] ModifierVirtualKeys = {
            VK_LCONTROL, VK_RCONTROL, VK_LSHIFT, VK_RSHIFT, VK_LMENU, VK_RMENU, VK_LWIN, VK_RWIN
        };

        // These share a scan code with another key and are only told apart by the extended flag.
        private static readonly int[] ExtendedModifierVirtualKeys = { VK_RCONTROL, VK_RMENU, VK_LWIN, VK_RWIN };

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public static void KeyDown(int virtualKey, int scanCode, bool isExtended) {
            int flags = isExtended ? KEYEVENTF_EXTENDEDKEY : 0;
            keybd_event((byte)virtualKey, (byte)scanCode, flags, UIntPtr.Zero);
        }

        public static void KeyUp(int virtualKey, int scanCode, bool isExtended) {
            int flags = KEYEVENTF_KEYUP | (isExtended ? KEYEVENTF_EXTENDEDKEY : 0);
            keybd_event((byte)virtualKey, (byte)scanCode, flags, UIntPtr.Zero);
        }

        public static void ModifierDown(int virtualKey) {
            KeyDown(virtualKey, GetScanCode(virtualKey), ExtendedModifierVirtualKeys.Contains(virtualKey));
        }

        public static void ModifierUp(int virtualKey) {
            KeyUp(virtualKey, GetScanCode(virtualKey), ExtendedModifierVirtualKeys.Contains(virtualKey));
        }

        public static bool IsModifier(int virtualKey) {
            return ModifierVirtualKeys.Contains(virtualKey);
        }

        public static bool IsKeyDown(int virtualKey) {
            return (GetAsyncKeyState(virtualKey) & KeyDownBit) != 0;
        }

        public static List<int> GetHeldModifiers() {
            return ModifierVirtualKeys.Where(IsKeyDown).ToList();
        }

        public static string GetKeyName(int virtualKey) {
            Key key = KeyInterop.KeyFromVirtualKey(virtualKey);
            return key == Key.None ? $"VK {virtualKey}" : key.ToString();
        }

        private static int GetScanCode(int virtualKey) {
            return (int)MapVirtualKey((uint)virtualKey, MAPVK_VK_TO_VSC);
        }
    }
}
