using AutomationServices.EmguCv;
using AutomationServices.EmguCv.Helper;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Selenium
{
    class Program
    {
        private static Mutex mutex;
        static void Main(string[] args)
        {
            //Test(); return;
            //单例程序
            if (SingleProcess())
                return;


#if DEBUG
            //立即执行 Debug
            AutomatedSelenium selenium = new AutomatedSelenium()
            {
                ShowBrowserWnd = true,
            };
            selenium.StartTask();
#else
            //定时任务
            new ScheduledTask().StartExecuteTask();
            //开机启动
            StartUp(); 
#endif

            //隐藏控制台窗口
            Console.Title = "QQ农场牧场自动化";
            ShowWindow(FindWindow(null, "QQ农场牧场自动化"), 0);

            Console.ReadLine();
        }


        public static void Test()
        {
            //aaa();
            string minImg = AppDomain.CurrentDomain.BaseDirectory + "min.png";
            string maxImg = AppDomain.CurrentDomain.BaseDirectory + "max.png";

            //EmguCvHelper.GetMatchPos(maxImg, @"C:\Users\YR\Desktop\r.png" );
            EmguCvHelper.SliderVerifi(maxImg, @"C:\Users\YR\Desktop\r.png" );
        }


        static void aaa()
        {
            string minImg = AppDomain.CurrentDomain.BaseDirectory + "min.png";
            //读入原图，这张原图尺寸为533*300
            Bitmap src_jpg = new Bitmap(minImg);

            //原图中的要抠出的一小块图，这一小块的左上角的坐标为(50, 0)，长为300，高为300
            Rectangle srcRect = new Rectangle(145, 488, 110, 95);

            //新图在画布上的左上角坐标为(0, 0)，新图长300，高300
            Rectangle destRect = new Rectangle(0, 0, 110, 95);

            //放置新图的画布，照搬新图的的大小
            Bitmap new_jpg = new Bitmap(destRect.Width, destRect.Height);

            //g就像一只画笔，准备在new_jpg上作画
            Graphics g = Graphics.FromImage(new_jpg);                        
            g.DrawImage(src_jpg, destRect, srcRect, GraphicsUnit.Pixel);

            //保存图片
            new_jpg.Save(@"C:\Users\YR\Desktop\r.png");

            //类似于关闭文件流，否则程序不终止，"pikachu.jpg"就处于被占用的状态
            src_jpg.Dispose();                       
        }

        static bool SingleProcess()
        {
            mutex = new Mutex(true, "MySeleniumConsole");
            if (!mutex.WaitOne(0, false))
            {
                return true;
            }
            return false;
        }

        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //static void Main(string[] args)
        //{
        //    Console.Title = "SeleniumConsole";
        //    IntPtr intptr = FindWindow("ConsoleWindowClass", "SeleniumConsole");
        //    if (intptr != IntPtr.Zero)
        //    {
        //        ShowWindow(intptr, 0);//隐藏这个窗⼝
        //    }
        //    string x;
        //    x = Console.ReadLine();
        //}



        //[DllImport("User32.dll")]
        //public static extern int ShowWindow(int hwnd, int nCmdShow);
        //[DllImport("User32.dll")]
        //public static extern int FindWindow(string lpClassName, string lpWindowName);
        //private const int SW_HIDE = 0;
        //private const int SW_NORMAL = 1;
        //private const int SW_MAXIMIZE = 3;
        //private const int SW_SHOWNOACTIVATE = 4;
        //private const int SW_SHOW = 5;
        //private const int SW_MINIMIZE = 6;
        //private const int SW_RESTORE = 9;
        //private const int SW_SHOWDEFAULT = 10;


        private static void StartUp()
        {
            //获取程序执行路径..
            string starupPath = AppDomain.CurrentDomain.BaseDirectory + "QQ农场牧场自动化.exe";
            //class Micosoft.Win32.RegistryKey. 表示Window注册表中项级节点,此类是注册表装.
            //RegistryKey loca = Registry.LocalMachine;
            RegistryKey loca = Registry.CurrentUser;
            RegistryKey run = loca.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

            try
            {
                //SetValue:存储值的名称
                run.SetValue("QQFarmAutomated", starupPath);
                loca.Close();
            }
            catch (Exception ee)
            {
                throw ee;
            }

        }
    }

    public class WebDownload : WebClient
    {
        /// <summary>
        /// Time in milliseconds
        /// </summary>
        public int Timeout { get; set; }

        public WebDownload() : this(5000) { }

        public WebDownload(int timeout)
        {
            this.Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;// https证书
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            var request = base.GetWebRequest(address);
            if (request != null)
            {
                request.Timeout = this.Timeout;
            }
            return request;
        }
    }




 
}
