using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace SkipDrama_YuanShen
{
    [Flags]
    internal enum GlobalHotkeyModifiers : uint
    {
        Control = 0x0002
    }

    internal sealed class GlobalHotkeyManager : IDisposable
    {
        private const int WmHotkey = 0x0312;
        private readonly IntPtr _windowHandle;
        private readonly HwndSource _source;
        private readonly Dictionary<int, Action> _callbacks = new Dictionary<int, Action>();
        private int _nextId = 100;
        private bool _disposed;

        internal GlobalHotkeyManager(IntPtr windowHandle)
        {
            _windowHandle = windowHandle;
            _source = HwndSource.FromHwnd(windowHandle) ?? throw new InvalidOperationException("无法获取窗口消息源。");
            _source.AddHook(WindowProcedure);
        }

        internal void Register(GlobalHotkeyModifiers modifiers, Key key, Action callback)
        {
            var id = _nextId++;
            var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
            if (!RegisterHotKey(_windowHandle, id, modifiers, virtualKey))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"注册快捷键 {modifiers}+{key} 失败。");
            }

            _callbacks.Add(id, callback);
        }

        private IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == WmHotkey && _callbacks.TryGetValue(wParam.ToInt32(), out var callback))
            {
                callback();
                handled = true;
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var id in _callbacks.Keys)
            {
                UnregisterHotKey(_windowHandle, id);
            }

            _callbacks.Clear();
            _source.RemoveHook(WindowProcedure);
            _disposed = true;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr window, int id, GlobalHotkeyModifiers modifiers, uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr window, int id);
    }
}
