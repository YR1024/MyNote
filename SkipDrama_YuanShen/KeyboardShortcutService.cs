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
        private const uint ExtendedKey = 0x0001;
        private const uint MapVkToVscEx = 4;
        private const ushort VirtualKeyAlt = 0x12;
        private const ushort VirtualKeyLeftAlt = 0xA4;
        private const ushort VirtualKeyF1 = 0x70;
        private const ushort ScanCodeAlt = 0x38;
        private const ushort ScanCodeF1 = 0x3B;
        private static readonly int InputSize = IntPtr.Size == 8 ? 40 : 28;

        internal static string SendNvidiaScreenshotShortcut()
        {
            try
            {
                return SendRemapperStyleShortcut();
            }
            catch (Exception first)
            {
                try
                {
                    return "SendInput RemapperStyle 失败: " + first.Message + "; " + SendVirtualKeyShortcut();
                }
                catch (Exception second)
                {
                    throw new InvalidOperationException("SendInput RemapperStyle 失败: " + first.Message + "; SendInput VK 失败: " + second.Message, second);
                }
            }
        }

        private static string SendVirtualKeyShortcut()
        {
            var inputs = new[]
            {
                CreateVirtualKeyInput(VirtualKeyLeftAlt, false),
                CreateVirtualKeyInput(VirtualKeyF1, false)
            };
            var down = SendInputs(inputs);

            Thread.Sleep(80);

            inputs = new[]
            {
                CreateVirtualKeyInput(VirtualKeyF1, true),
                CreateVirtualKeyInput(VirtualKeyLeftAlt, true)
            };
            var up = SendInputs(inputs);
            return "SendInput VK_LMENU+F1 成功: down " + down + "/2, up " + up + "/2";
        }

        private static string SendRemapperStyleShortcut()
        {
            SendScanCodeKey(VirtualKeyAlt, false);
            SendScanCodeKey(VirtualKeyF1, false);

            Thread.Sleep(1);

            SendScanCodeKey(VirtualKeyAlt, true);
            SendScanCodeKey(VirtualKeyF1, true);
            return "SendInput RemapperStyle Alt+F1 成功: down 2/2, up 2/2";
        }

        private static string SendScanCodeShortcut()
        {
            var inputs = new[]
            {
                CreateScanCodeInput(ScanCodeAlt, false),
                CreateScanCodeInput(ScanCodeF1, false)
            };
            var down = SendInputs(inputs);

            Thread.Sleep(100);

            inputs = new[]
            {
                CreateScanCodeInput(ScanCodeF1, true),
                CreateScanCodeInput(ScanCodeAlt, true)
            };
            var up = SendInputs(inputs);
            return "SendInput ScanCode Alt+F1 成功: down " + down + "/2, up " + up + "/2";
        }

        private static void SendScanCodeKey(ushort virtualKey, bool keyUp)
        {
            var scanCode = MapVirtualKey(virtualKey, MapVkToVscEx);
            var flags = ScanCode | (keyUp ? KeyUp : 0);
            if (((scanCode >> 8) & 0xE0) != 0)
            {
                flags |= ExtendedKey;
            }

            var input = new[]
            {
                new Input
                {
                    Type = InputKeyboard,
                    Data = new InputUnion
                    {
                        Keyboard = new KeyboardInput
                        {
                            VirtualKey = 0,
                            ScanCode = (ushort)(scanCode & 0xFF),
                            Flags = flags
                        }
                    }
                }
            };
            SendInputs(input);
        }

        private static Input CreateVirtualKeyInput(ushort virtualKey, bool keyUp)
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

        private static uint SendInputs(Input[] inputs)
        {
            var sent = SendInput((uint)inputs.Length, inputs, InputSize);
            if (sent != inputs.Length)
            {
                var error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, "发送 NVIDIA 截图快捷键 Alt+F1 失败。cbSize=" + InputSize + ", sent=" + sent + "/" + inputs.Length + ", error=" + error);
            }

            return sent;
        }

        private static Input CreateScanCodeInput(ushort scanCode, bool keyUp)
        {
            return new Input
            {
                Type = InputKeyboard,
                Data = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        VirtualKey = 0,
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
            internal MouseInput Mouse;

            [FieldOffset(0)]
            internal KeyboardInput Keyboard;

            [FieldOffset(0)]
            internal HardwareInput Hardware;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            internal int X;
            internal int Y;
            internal uint MouseData;
            internal uint Flags;
            internal uint Time;
            internal UIntPtr ExtraInfo;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct HardwareInput
        {
            internal uint Message;
            internal ushort ParamLow;
            internal ushort ParamHigh;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint code, uint mapType);
    }
}
