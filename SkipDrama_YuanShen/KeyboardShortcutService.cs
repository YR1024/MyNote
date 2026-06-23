using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace SkipDrama_YuanShen
{
    internal static class KeyboardShortcutService
    {
        private const uint InputKeyboard = 1;
        private const uint KeyUp = 0x0002;
        private const uint ScanCode = 0x0008;
        private const ushort VirtualKeyAlt = 0x12;
        private const ushort VirtualKeyF1 = 0x70;
        private const ushort ScanCodeAlt = 0x38;
        private const ushort ScanCodeF1 = 0x3B;

        internal static void SendNvidiaScreenshotShortcut()
        {
            SendScanCodeShortcut();
        }

        private static void SendScanCodeShortcut()
        {
            var inputs = new[]
            {
                CreateKeyboardInput(VirtualKeyAlt, ScanCodeAlt, false)
            };
            SendInputs(inputs);

            Thread.Sleep(30);

            inputs = new[]
            {
                CreateKeyboardInput(VirtualKeyF1, ScanCodeF1, false)
            };
            SendInputs(inputs);

            Thread.Sleep(50);

            inputs = new[]
            {
                CreateKeyboardInput(VirtualKeyF1, ScanCodeF1, true),
                CreateKeyboardInput(VirtualKeyAlt, ScanCodeAlt, true)
            };
            SendInputs(inputs);
        }

        private static void SendInputs(Input[] inputs)
        {
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
            if (sent != inputs.Length)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "发送 NVIDIA 截图快捷键 Alt+F1 失败。");
            }
        }

        private static Input CreateKeyboardInput(ushort virtualKey, ushort scanCode, bool keyUp)
        {
            return new Input
            {
                Type = InputKeyboard,
                Data = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        VirtualKey = virtualKey,
                        ScanCode = scanCode,
                        Flags = ScanCode | (keyUp ? KeyUp : 0)
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
