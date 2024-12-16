using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Windows.System;

namespace Rememory.Helper
{
    public static class KeyboardHelper
    {
        public static readonly List<VirtualKey> ModifierKeys = [
            VirtualKey.LeftWindows, VirtualKey.RightWindows,
            VirtualKey.Control, VirtualKey.LeftControl, VirtualKey.RightControl,
            VirtualKey.Shift, VirtualKey.LeftShift, VirtualKey.RightShift,
            VirtualKey.Menu, VirtualKey.LeftMenu, VirtualKey.RightMenu];

        public static void MultiKeyAction(VirtualKey[] keys, KeyAction action)
        {
            int inputsCount = action == KeyAction.DownUp ? keys.Length * 2 : keys.Length;
            INPUT[] inputs = new INPUT[inputsCount];

            for (int i = 0; i < keys.Length; ++i)
            {
                if (action == KeyAction.Down || action == KeyAction.DownUp)
                {
                    inputs[i].Type = 1;
                    inputs[i].Data.Keyboard = new KEYBDINPUT()
                    {
                        Vk = (ushort)keys[i],
                        Scan = 0,
                        Flags = 0, // Key down
                        Time = 0,
                        ExtraInfo = IntPtr.Zero,
                    };
                }

                if (action == KeyAction.Up || action == KeyAction.DownUp)
                {
                    inputs[action == KeyAction.DownUp ? keys.Length + i : i].Type = 1;
                    inputs[action == KeyAction.DownUp ? keys.Length + i : i].Data.Keyboard = new KEYBDINPUT()
                    {
                        Vk = (ushort)keys[i],
                        Scan = 0,
                        Flags = 2, // Key up
                        Time = 0,
                        ExtraInfo = IntPtr.Zero,
                    };
                }
            }

            if (NativeHelper.SendInput(Convert.ToUInt32(inputs.Length), inputs, Marshal.SizeOf(typeof(INPUT))) == 0)
                throw new Exception("SendInput failed");
        }

        public static string ShortcutToString(IEnumerable<int> keys, string separator)
        {
            return string.Join(separator, keys.OrderBy(key =>
            {
                int index = ModifierKeys.IndexOf((VirtualKey)key);
                return index == -1 ? ModifierKeys.Count : index;
            }).Select(VirtualKeyToString));
        }

        public static string VirtualKeyToString(int key)
        {
            return key switch
            {
                0x11 or 0xA2 or 0xA3 => "Ctrl",
                0x12 or 0xA4 or 0xA5 => "Alt",
                >= 0x30 and <= 0x39 => $"{key % 0x10}",                 // VK_NUMBER...
                >= 0x60 and <= 0x69 => $"NumPad {key % 0x10}",          // VK_NUMPAD...
                0x6E => ".",                                            // VK_DECIMAL
                0x5B or 0x5C => "Win",
                0xAD => "Volume Mute",
                0xAE => "Volume Down",
                0xAF => "Volume Up",
                0xB0 => "Next Track",
                0xB1 => "Previous Track",
                0xB2 => "Stop Media",
                0xB3 => "Play/Pause Media",
                >= 0xBA and <= 0xE2 => GetCharFromKey(key),             // VK_OEM...
                _ => ((VirtualKey)key).ToString()
            };
        }

        public static string GetCharFromKey(int virtualKey)
        {
            var charBuffer = new StringBuilder(2);
            byte[] keyboardState = new byte[256];

            if (NativeHelper.GetKeyboardState(keyboardState))
            {
                uint scanCode = NativeHelper.MapVirtualKey((uint)virtualKey, 0);
                int result = NativeHelper.ToUnicode((uint)virtualKey, scanCode, keyboardState, charBuffer, charBuffer.Capacity, 0);

                if (result > 0)
                {
                    return charBuffer.ToString();
                }
            }

            return string.Empty;
        }

        public enum KeyAction
        {
            Down,
            Up,
            DownUp
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            public uint Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            public uint Msg;
            public ushort ParamL;
            public ushort ParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort Vk;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LowLevelKeyboardInputEvent
        {
            public int VirtualCode;
            public int HardwareScanCode;
            public int Flags;
            public int TimeStamp;
            public IntPtr AdditionalInformation;
        }
    }
}
