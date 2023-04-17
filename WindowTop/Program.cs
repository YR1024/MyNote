using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static WindowTop.KeyboardHook;
using Application = System.Windows.Forms.Application;

namespace WindowTop
{
    internal class Program
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static void Main(string[] args)
        {
            // 创建托盘图标
            var notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.Text = "Window TopMost";
            notifyIcon.Visible = true;

            // 设置托盘图标的快捷菜单
            var contextMenu = new ContextMenu();
            var menuItemExit = new MenuItem("退出");
            menuItemExit.Click += (s, e) => Application.Exit();
            contextMenu.MenuItems.Add(menuItemExit);
            notifyIcon.ContextMenu = contextMenu;

            // 注册鼠标单击事件和快捷键
            //MouseHook.RegisterMouseClickEvent(MouseButtons.Left, HandleMouseClick);
            KeyboardHook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Shift, Keys.T, HandleHotKey);

            // 运行消息循环
            Application.Run();
        }

        private static void HandleMouseClick()
        {
            // 获取当前活动窗口句柄
            var hWnd = GetForegroundWindow();

            // 将当前活动窗口置顶
            SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0, 0x0002 | 0x0001);
        }

        private static void HandleHotKey()
        {
            // 获取当前活动窗口句柄
            var hWnd = GetForegroundWindow();

            // 将当前活动窗口置顶
            SetWindowPos(hWnd, new IntPtr(-1), 0, 0, 0, 0, 0x0002 | 0x0001);
        }
    }


    static class MouseHook
    {
        public delegate void MouseEventHandler();

        private static IntPtr _hookId = IntPtr.Zero;
        private static MouseEventHandler _mouseClickEvent;

        public static void RegisterMouseClickEvent(MouseButtons button, MouseEventHandler handler)
        {
            _mouseClickEvent = handler;

            _hookId = SetHook(HookType.WH_MOUSE_LL, MouseProc);

            Application.ApplicationExit += (s, e) => UnhookWindowsHookEx(_hookId);
        }

        private static IntPtr SetHook(HookType hookType, HookProc hookProc)
        {
            using (ProcessModule module = Process.GetCurrentProcess().MainModule)
            {
                return SetWindowsHookEx((int)hookType, hookProc, GetModuleHandle(module.ModuleName), 0);
            }
        }

        private static IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                _mouseClickEvent?.Invoke();
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        #region Windows API

        private const int WM_LBUTTONDOWN = 0x0201;

        private enum HookType : int
        {
            WH_MOUSE_LL = 14,
        }

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
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }


    static class KeyboardHook
    {
        public delegate void HotKeyEventHandler();

        private static IntPtr _hookId = IntPtr.Zero;
        private static HotKeyEventHandler _hotKeyEvent;

        public static void RegisterHotKey(ModifierKeys modifiers, Keys key, HotKeyEventHandler handler)
        {
            _hotKeyEvent = handler;

            _hookId = SetHook(HookType.WH_KEYBOARD_LL, KeyboardProc);

            RegisterHotKey(IntPtr.Zero, 0, (uint)modifiers, (uint)key);

            Application.ApplicationExit += (s, e) =>
            {
                UnregisterHotKey(IntPtr.Zero, 0);
                UnhookWindowsHookEx(_hookId);
            };
        }

        private static IntPtr SetHook(HookType hookType, HookProc hookProc)
        {
            using (ProcessModule module = Process.GetCurrentProcess().MainModule)
            {
                return SetWindowsHookEx((int)hookType, hookProc, GetModuleHandle(module.ModuleName), 0);
            }
        }

        private static IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_HOTKEY)
            {
                _hotKeyEvent?.Invoke();
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        #region Windows API

        private const int WM_HOTKEY = 0x0312;

        private enum HookType : int
        {
            WH_KEYBOARD_LL = 13,
        }

        [Flags]
        public enum ModifierKeys : uint
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            Win = 8
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion
    }
}
