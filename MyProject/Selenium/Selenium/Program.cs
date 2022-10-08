using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
            //单例程序
            if (SingleProcess())
                return;

            //定时任务
            new ScheduledTask().StartExecuteTask();

            //立即执行 Debug
            //AutomatedSelenium selenium = new AutomatedSelenium()
            //{
            //    ShowBrowserWnd = true,
            //};
            //selenium.StartTask();

            //隐藏控制台窗口
            Console.Title = "QQ农场牧场自动化";
            ShowWindow(FindWindow(null, "QQ农场牧场自动化"), 0);

            //开机启动
            StartUp(); 
            Console.ReadLine();
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
