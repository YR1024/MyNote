using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopIconTool.Helper
{
    public class HookTool
    {

        // 钩子类型
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;

        //键盘钩子
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101; 

        // 钩子句柄
        private static IntPtr _hookID = IntPtr.Zero;
        private static IntPtr _hookKbID = IntPtr.Zero;


        // 钩子回调委托，需保存引用防止被GC回收
        private static LowLevelMouseProc _proc = HookCallback;

        // 鼠标移动事件
        public static event Action<int, int> MouseMoved;
        // 鼠标左键按下事件
        public static event Action<int, int> MouseLeftButtonDown;
        // 键盘按下事件
        //public static event Action<Keys, int> KeyDown;
        public static event Func<Keys, int, bool> KeyDown;

        // 设置钩子
        public static void Start()
        {
            _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(null), 0);
        }

        // 卸载钩子
        public static void Stop()
        {
            UnhookWindowsHookEx(_hookID);
            UnhookWindowsHookEx(_hookKbID);
        }


        // 注册键盘钩子
        public static void StartKeyboardHook()
        {
            //_hookKbID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
            _hookKbID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
        }
        // 卸载键盘钩子
        public static void StopKeyboardHook()
        {
            UnhookWindowsHookEx(_hookKbID);
        }


        // 钩子回调
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_MOUSEMOVE)
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    MouseMoved?.Invoke(hookStruct.pt.x, hookStruct.pt.y);
                }
                else if (wParam == (IntPtr)WM_LBUTTONDOWN)
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    MouseLeftButtonDown?.Invoke(hookStruct.pt.x, hookStruct.pt.y);
                }
                else if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    // 修正结构体解析
                    //KBDLLHOOKSTRUCT kbStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    //bool shouldBlock = KeyDown?.Invoke((Keys)kbStruct.vkCode, kbStruct.flags) ?? false;
                    //bool shouldBlock = KeyDown?.Invoke((Keys)Marshal.ReadInt32(lParam), wParam.ToInt32()) ?? false;

                    KBDLLHOOKSTRUCT kbStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    bool shouldBlock = KeyDown?.Invoke((Keys)kbStruct.vkCode,(int)kbStruct.flags ) ?? false;

                    if (shouldBlock)
                    {
                        return (IntPtr)1; // 正确返回阻断值
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }



        // Win32结构体
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // Win32 API声明
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


        // 在目标进程中注入DLL
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr LoadLibrary(string lpFileName);

 

    }
}
