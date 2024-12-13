using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DesktopIconTool
{
    public class Win32Helper
    {
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        #region 当前是否有激活窗口
        public delegate bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsCallback lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern long GetWindowLong(IntPtr hWnd, int nIndex);

        private static bool EnumWindowsCallbackMethod(IntPtr hWnd, IntPtr lParam)
        {
            // 检查窗口是否可见和非最小化
            if (IsWindowVisible(hWnd) && !IsIconic(hWnd))
            {
                // 获取窗口标题
                const int MaxWindowTitleLength = 256;
                StringBuilder windowTitleBuilder = new StringBuilder(MaxWindowTitleLength);
                GetWindowText(hWnd, windowTitleBuilder, MaxWindowTitleLength);

                // 输出窗口标题和进程名
                Console.WriteLine("窗口标题: " + windowTitleBuilder.ToString());
                uint processId;
                GetWindowThreadProcessId(hWnd, out processId);
                Process process = Process.GetProcessById((int)processId);
                Console.WriteLine("进程名: " + process.ProcessName);
                Console.WriteLine("-----------------------------------------");

                // 如果有任何窗口是激活或非最小化状态，则返回 true
                if (hWnd == GetForegroundWindow())
                {
                    return true;
                }
            }

            return false;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left; //最左坐标
            public int Top; //最上坐标
            public int Right; //最右坐标
            public int Bottom; //最下坐标
        }

        public static Rectangle GetWindowLocationSize(IntPtr h)
        {
            RECT fx = new RECT();
            GetWindowRect(h, ref fx);//h为窗口句柄
            int width = fx.Right - fx.Left;                        //窗口的宽度
            int height = fx.Bottom - fx.Top;                   //窗口的高度
            int x = fx.Left;
            int y = fx.Top;
            return new Rectangle(x, y, width, height);
        }


        const int GWL_EXSTYLE = (-20); //扩展窗口样式
        const int GWL_STYLE = (-16); //窗口样式
        const uint WS_VISIBLE = 0x10000000;

        public static bool IsHasWindowActive()
        {
            //var style = GetWindowLong(hwnd, GWL.GWL_EXSTYLE);

            //List<IntPtr> WindowHandleList = new List<IntPtr>();
            List<Process> Processlist = new List<Process>();
            List<WindowInfo> WindowInfolist = new List<WindowInfo>();
            // 获取所有进程
            Process[] processes = Process.GetProcesses();

            // 遍历每个进程并检查MainWindowHandle属性
            foreach (Process process in processes)
            {
                IntPtr mainWindowHandle = process.MainWindowHandle;

                // 如果MainWindowHandle不为空，则表示该进程有前台窗口
                if (mainWindowHandle != IntPtr.Zero)
                {
                    //Console.WriteLine("进程名称: {0}   ", process.ProcessName);
                    //Console.WriteLine("窗口标题: {0}   ", process.MainWindowTitle);
                    //Processlist.Add(process);
                    //Console.WriteLine("显示状态: {0}   ", IsWindowVisible(mainWindowHandle));
                    //Console.WriteLine("最小化: {0}   ", IsIconic(mainWindowHandle));
                    //var exstyle = GetWindowLong(mainWindowHandle, GWL_EXSTYLE);
                    //var style = GetWindowLong(mainWindowHandle, GWL_STYLE);
                    //Console.WriteLine("Exstyle: {0}   ", exstyle);
                    //Console.WriteLine("Style: {0}   ", style);
                    //bool bVisible = (GetWindowLong(mainWindowHandle, GWL_STYLE) & WS_VISIBLE) != 0;
                    //Console.WriteLine("bVisible: {0}   ", bVisible);
                    //Console.WriteLine("");

                    WindowInfo windowInfo = new WindowInfo()
                    {
                        Process = process.ProcessName,
                        Title = process.MainWindowTitle,
                        Handle = process.MainWindowHandle,
                        IsMinimize = IsIconic(mainWindowHandle),
                        Exstyle = GetWindowLong(mainWindowHandle, GWL_EXSTYLE),
                        Style = GetWindowLong(mainWindowHandle, GWL_STYLE),
                        //Rect = GetWindowLocationSize(mainWindowHandle),
                    };
                    WindowInfolist.Add(windowInfo);
                }
            }

            foreach (var wi in WindowInfolist)
            {
                if (wi.Title == "媒体播放器" || string.IsNullOrWhiteSpace(wi.Title))
                {
                    continue;
                }
                if (wi.Style == 6777995264)
                {
                    continue;
                }
                if (!wi.IsMinimize)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion







    }


    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }


    public class WindowInfo
    {
        public string Process { get; set; }

        public string Title { get; set; }

        public IntPtr Handle { get; set; }

        public bool IsMinimize { get; set; }

        public long Exstyle { get; set; }

        public long Style { get; set; }

        public Rectangle Rect { get; set; }
    }
}
