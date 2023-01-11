using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace AutomationServices.EmguCv.Helper
{
    public class Win32Helper
    {
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);


        [DllImport("User32.dll", EntryPoint = "MoveWindow")]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool BRePaint);


        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);


        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter,
                                                 string lpszClass, string lpszWindow);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, int lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetClassNameW(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, ref Rect lpRect);

        public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        public struct WindowInfo
        {
            public IntPtr hWnd;//句柄
            public string szWindowName;//窗口名
            public string szClassName;//类名
            public System.Drawing.Rectangle Rect;//位置大小信息
        }
        public static List<WindowInfo> EnumChildWindowsCallback(IntPtr handle)
        {
            //用于保存句柄列表
            List<WindowInfo> wndList = new List<WindowInfo>();
            EnumChildWindows(handle, delegate (IntPtr hWnd, int lParam)
            {
                WindowInfo wnd = new WindowInfo();
                StringBuilder sb = new StringBuilder(256);
                //get hwnd
                wnd.hWnd = hWnd;
                //get window name
                GetWindowTextW(hWnd, sb, sb.Capacity);
                wnd.szWindowName = sb.ToString();
                //get window class
                GetClassNameW(hWnd, sb, sb.Capacity);
                wnd.szClassName = sb.ToString();
                Rect rect = new Rect();
                if (GetWindowRect(hWnd, ref rect) == true)
                {
                    wnd.Rect = new System.Drawing.Rectangle((int)rect.X, (int)rect.Y,
                                                            (int)rect.Width, (int)rect.Height);
                }
                wndList.Add(wnd);
                return true;
            }, 0);
            //这里已经得到句柄列表 “wndList” ，对wndList进行筛选即可。
            return wndList;
        }



        /// <summary>
        /// 发送点击事件
        /// </summary>
        /// <param name="hwnd">控件句柄</param>
        public static void SendClick(IntPtr hwnd)
        {
            SendMessage(hwnd, WM_CLICK, 0, 0);
        }
        /// <summary>
        /// 发送点击事件
        /// </summary>
        /// <param name="hwnd">控件句柄</param>
        /// <param name="x">鼠标位置x</param>
        /// <param name="y">鼠标位置y</param>
        public static void SendClick(IntPtr hwnd, int X, int Y)
        {
            int lparm = (Y << 16) + X;
            SendMessage(hwnd, WM_CLICK, 0, lparm);
        }

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        /// <summary>
        /// 点击消息
        /// </summary>
        const uint WM_CLICK = 0xF5;









        /// <summary>
        /// 
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="nWidth"></param>
        /// <param name="nHeight"></param>
        /// <param name="bRepaint"></param>
        /// <returns></returns>
        //[System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "MoveWindow")]
        //public static extern bool MoveWindow(System.IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        /// <summary>
        /// 打开窗体方法，fileName是的窗体名称，包含路径
        /// </summary>
        /// <param name="fileName"></param>
        public static void OpenAndSetWindow(IntPtr pHandle, int windowWidth = 800, int windowHeight = 600, int x = 0, int y = 0)
        {
            //Process p = new Process();//新建进程 

            //p.StartInfo.FileName = fileName;//设置进程名字 

            //p.StartInfo.CreateNoWindow = true;

            //p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            //p.Start();

            MoveWindow(pHandle, x, y, windowWidth, windowHeight, true);

            //p.MainWindowHandle是你要移动的窗口的句柄；
            //200,300是移动后窗口左上角的横纵坐标；
            //500,400是移动后窗口的宽度和高度；
            //true表示移动后的窗口是需要重画 
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="exeName"></param>
        /// <param name="operType">
        /// operType参数如下：
        /// 0: 隐藏, 并且任务栏也没有最小化图标  
        /// 1: 用最近的大小和位置显示, 激活  
        /// 2: 最小化, 激活  
        /// 3: 最大化, 激活  
        /// 4: 用最近的大小和位置显示, 不激活  
        /// 5: 同 1  
        /// 6: 最小化, 不激活  
        /// 7: 同 3  
        /// 8: 同 3  
        /// 9: 同 1  
        /// 10: 同 1 
        /// </param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]

        public static extern int WinExec(string exeName, int operType);

        #region 无边框


        public static int SetWindowNotBorder(IntPtr wndHandle)
        {
            //var wnd = FindWindowA(null, "窗口标题");
            //Int32 wndStyle = GetWindowLong(wnd,GWL_STYLE);
            //wndStyle &= ~WS_BORDER;
            //wndStyle &= ~WS_THICKFRAME;
            //SetWindowLong(wnd, GWL_STYLE, wndStyle);

            int style = GetWindowLong(wndHandle, GWL_STYLE);
            return SetWindowLong(wndHandle, GWL_STYLE, (style & ~WS_CAPTION));
            //Height = ClientRectangle.Height;
        }

        //const int WS_THICKFRAME = 262144;
        //const int WS_BORDER = 8388608;
        //[DllImport("user32.dll")]
        //public static extern IntPtr FindWindowA(string lpClassName, string lpWindowName);
        //[DllImport("user32.dll")]
        //public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        //[DllImport("user32.dll")]
        //public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);


        [DllImport("USER32.DLL")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("USER32.DLL")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        public static int GWL_STYLE = -16;
        public static int WS_CHILD = 0x40000000;
        public static int WS_BORDER = 0x00800000;
        public static int WS_DLGFRAME = 0x00400000;
        public static int WS_CAPTION = WS_BORDER | WS_DLGFRAME;
        #endregion
    }
}
