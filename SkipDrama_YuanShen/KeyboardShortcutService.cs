using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SkipDrama_YuanShen
{
    internal static class KeyboardShortcutService
    {
        private const uint InputKeyboard = 1;
        private const uint KeyUp = 0x0002;
        private const ushort VirtualKeyAlt = 0x12;
        private const ushort VirtualKeyF1 = 0x70;

        internal static void SendNvidiaScreenshotShortcut()
        {
            var inputs = new[]
            {
                CreateKeyboardInput(VirtualKeyAlt, false),
                CreateKeyboardInput(VirtualKeyF1, false),
                CreateKeyboardInput(VirtualKeyF1, true),
                CreateKeyboardInput(VirtualKeyAlt, true)
            };

            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
            if (sent != inputs.Length)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "发送 NVIDIA 截图快捷键 Alt+F1 失败。");
            }
        }

        private static Input CreateKeyboardInput(ushort virtualKey, bool keyUp)
        {
            return new Input
            {
                Type = InputKeyboard,
                Data = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        VirtualKey = virtualKey,
                        Flags = keyUp ? KeyUp : 0
                    }
                }
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            internal uint Type;
            internal InputUnion Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            internal KeyboardInput Keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            internal ushort VirtualKey;
            internal ushort ScanCode;
            internal uint Flags;
            internal uint Time;
            internal UIntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);
    }
}
