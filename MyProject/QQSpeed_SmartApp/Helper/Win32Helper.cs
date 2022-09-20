using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace QQSpeed_SmartApp.Helper
{
    public class Win32Helper
    {
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
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "MoveWindow")]
        public static extern bool MoveWindow(System.IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        /// <summary>
        /// 打开窗体方法，fileName是的窗体名称，包含路径
        /// </summary>
        /// <param name="fileName"></param>
        public static void OpenAndSetWindow(IntPtr pHandle,int windowWidth = 800, int windowHeight = 600, int x = 0, int y = 0)
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
