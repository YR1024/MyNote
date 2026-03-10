using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace WpfApp2
{
    public sealed class HidReportEventArgs : EventArgs
    {
        public HidReportEventArgs(byte[] report) => Report = report;
        public byte[] Report { get; }
    }

    /// <summary>
    /// 用 Raw Input 监听 HID 游戏手柄输入（后台也能收：RIDEV_INPUTSINK）
    /// </summary>
    public sealed class RawInputHidListener : IDisposable
    {
        public event EventHandler<HidReportEventArgs> HidReport;

        private readonly IntPtr _hwnd;
        private HwndSource _source;
        private bool _started;

        public RawInputHidListener(IntPtr hwnd)
        {
            _hwnd = hwnd;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;

            _source = HwndSource.FromHwnd(_hwnd) ?? throw new InvalidOperationException("HwndSource.FromHwnd failed.");
            _source.AddHook(WndProc);

            RegisterHidGamepad(_hwnd);
        }

        public void Dispose()
        {
            if (_source != null)
            {
                _source.RemoveHook(WndProc);
                _source = null;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_INPUT = 0x00FF;

            if (msg == WM_INPUT)
            {
                try
                {
                    var report = ReadHidReport(lParam);
                    if (report != null && report.Length > 0)
                    {
                        HidReport?.Invoke(this, new HidReportEventArgs(report));
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("WM_INPUT error: " + ex);
                }

                handled = true;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 注册 HID 游戏手柄（UsagePage=0x01, Usage=0x05）
        /// RIDEV_INPUTSINK 使窗口即使无焦点也能收到输入。
        /// </summary>
        private static void RegisterHidGamepad(IntPtr hwnd)
        {
            const ushort UsagePage_GenericDesktop = 0x01;
            const ushort Usage_GamePad = 0x05;

            const int RIDEV_INPUTSINK = 0x00000100;

            var rid = new RAWINPUTDEVICE[]
            {
                new RAWINPUTDEVICE
                {
                    usUsagePage = UsagePage_GenericDesktop,
                    usUsage = Usage_GamePad,
                    dwFlags = RIDEV_INPUTSINK,
                    hwndTarget = hwnd
                }
            };

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "RegisterRawInputDevices failed.");
            }
        }

        /// <summary>
        /// 从 WM_INPUT 的 lParam 读取 RAWINPUT，然后提取 HID 报文（bRawData）。
        /// 注意：这里返回的是“原始 HID 输入数据块”，你可以按你抓到的字节/bit去解析。
        /// </summary>
        private static byte[] ReadHidReport(IntPtr hRawInput)
        {
            uint dwSize = 0;

            // 第一次调用获取需要的 buffer 大小
            var headerSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();
            if (GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref dwSize, headerSize) != 0)
            {
                return null;
            }

            if (dwSize == 0) return null;

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            try
            {
                // 第二次调用真正读取数据
                uint read = GetRawInputData(hRawInput, RID_INPUT, buffer, ref dwSize, headerSize);
                if (read != dwSize) return null;

                // 读取 header 判断类型
                var header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);
                if (header.dwType != RIM_TYPEHID) return null;

                // RAWINPUT 是变长结构，我们用偏移手动解析 HID 区
                // 结构布局：RAWINPUTHEADER + RAWHID + bRawData
                // RAWHID: dwSizeHid, dwCount, bRawData...
                IntPtr pHid = buffer + Marshal.SizeOf<RAWINPUTHEADER>();
                var rawHid = Marshal.PtrToStructure<RAWHID>(pHid);

                int totalBytes = checked((int)(rawHid.dwSizeHid * rawHid.dwCount));
                if (totalBytes <= 0) return null;

                IntPtr pRawData = pHid + Marshal.SizeOf<RAWHID>();
                byte[] data = new byte[totalBytes];
                Marshal.Copy(pRawData, data, 0, totalBytes);

                // 注意：如果 dwCount > 1，data 里是多个 report 连在一起，你可以按 dwSizeHid 切分
                return data;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // ---------------- Win32 interop ----------------

        private const uint RID_INPUT = 0x10000003;
        private const uint RIM_TYPEHID = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public int dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        // 注意：RAWHID 后面紧跟变长 bRawData，因此这里只声明头部字段即可
        [StructLayout(LayoutKind.Sequential)]
        private struct RAWHID
        {
            public uint dwSizeHid;
            public uint dwCount;
        }

        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(
            [In] RAWINPUTDEVICE[] pRawInputDevices,
            uint uiNumDevices,
            uint cbSize);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(
            IntPtr hRawInput,
            uint uiCommand,
            IntPtr pData,
            ref uint pcbSize,
            uint cbSizeHeader);
    }
}
