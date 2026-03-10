using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace SkipDrama_YuanShen
{
    public static class ScreenshotHelper
    {

        static string basePath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) + "\\GamePadScreenshot\\";

        public static void Screenshot()
        {
            try
            {
                //创建目录
                if (!System.IO.Directory.Exists(basePath))
                {
                    System.IO.Directory.CreateDirectory(basePath);
                }

                // 获取屏幕分辨率
                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                // 创建与屏幕大小相同的 Bitmap
                using (Bitmap bmp = new Bitmap(screenWidth, screenHeight))
                {
                    // 从屏幕复制像素到 Bitmap
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                    }

                    // 生成文件名（带时间戳）
                    string fileName = $"{basePath}Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";

                    // 保存为 PNG 格式
                    bmp.Save(fileName, ImageFormat.Png);

                    //Console.WriteLine($"截图已保存到: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("截图失败: " + ex.Message);
            }
        }
   

        public static void MessageScreenshot()
        {
            //// 目标窗口标题
            //string windowTitle = "鸣潮";

            // 要模拟的按键
            char key = 'Q';

            // 调用方法发送按键消息
            //SendKeyDownUp(windowTitle, key);
            SendKeyDownUp2("Client-Win64-Shipping", key);
        }



        // 引入所需的Windows API函数
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        // 定义按键消息常量
        const int WM_KEYDOWN = 0x0100;
        const int WM_KEYUP = 0x0101;

        // 发送按键消息的方法
        private static void SendKeyDownUp(string windowTitle, char key)
        {
            // 获取窗口句柄
            IntPtr hWnd = FindWindow(null, windowTitle);
            if (hWnd != IntPtr.Zero)
            {
                // 发送按键按下消息
                SendMessage(hWnd, WM_KEYDOWN, (int)key, 0);

                // 发送按键释放消息
                SendMessage(hWnd, WM_KEYUP, (int)key, 0);
            }
        }





        // 引入Windows API
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // 通过进程名获取主窗口句柄
        public static IntPtr GetWindowHandleByProcessName(string processName)
        {
            IntPtr result = IntPtr.Zero;

            // 获取目标进程
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) return result;

            int targetPid = processes[0].Id;

            // 枚举窗口回调
            EnumWindowsProc callback = (hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out int windowPid);
                if (windowPid == targetPid)
                {
                    result = hWnd;
                    return false; // 找到后停止枚举
                }
                return true; // 继续枚举
            };

            EnumWindows(callback, IntPtr.Zero);
            return result;
        }
        private static void SendKeyDownUp2(string processName, char key)
        {
            // 通过进程名获取句柄
            IntPtr hWnd = GetWindowHandleByProcessName(processName);
            if (hWnd != IntPtr.Zero)
            {
                SendMessage(hWnd, WM_KEYDOWN, (int)key, 0);
                SendMessage(hWnd, WM_KEYUP, (int)key, 0);
            }
        }
    }
}




class Program
{

  
}
