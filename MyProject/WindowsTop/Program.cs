using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using static WindowsTop.Helper;

namespace WindowsTop
{
    class Program
    {
        private static Mutex mutex;

        [STAThread]
        static void Main(string[] args)
        {

            //单例程序
            if (SingleProcess())
                return;

            Console.Title = "WindowsTop";
            while(!ShowWindow(FindWindow(null, "WindowsTop"), 0)){

            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WindowsTool());

            mutex.ReleaseMutex();
        }
          


        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        static bool SingleProcess()
        {
            mutex = new Mutex(true, "WindowsTop");
            if (!mutex.WaitOne(0, false))
            {
                return true;
            }
            return false;
        }

    }



    public partial class WindowsTool : ApplicationContext
    {

        ResourceManager rm;

        Config _config;
        public WindowsTool()
        {
            Helper.rm = rm = Resources.ResourceManager;
            Helper.LoadConfig();
            _config = Helper.config;
            if (_config.StartUp)
            {
                Helper.StartUp();
            }

            // 注册全局热键
            RegisterHotKey(this.Handle, 1, 0, (int)HotKey);

            InitializeComponent();
            RunTask();
        }


        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const Keys HotKey = Keys.F9; // 可以修改为您想要的快捷键

        private bool isTopMost = false;

   

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == 1) // 接收到热键消息
            {
                IntPtr hwnd = GetForegroundWindow(); // 获取当前激活窗口的句柄

                // 根据当前置顶状态设置窗口置顶或取消置顶
                if (isTopMost)
                {
                    SetWindowPos(hwnd, new IntPtr(-2), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    isTopMost = false;
                }
                else
                {
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    isTopMost = true;
                }
            }

            base.WndProc(ref m);
        }

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        protected override void Dispose(bool disposing)
        {
        

            // 注销全局热键
            UnregisterHotKey(this.Handle, 1);
        }

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);



        void RunTask()
        {

            Task.Run(() =>
            {
                while(true)
                {
                    Task.Delay(1000);
                }

            });
        }

        public static bool ShowDesktopIcon = false;

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = CharSet.Auto)]
        public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        public delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);
        [DllImport("user32.dll")]
        private static extern int EnumWindows(EnumWindowsCallback callPtr, int lParam);

        private void 开机启动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                Helper.StartUp();
                _config.StartUp = true;
            }
            else
            {
                Helper.CancelStartUp();
                _config.StartUp = false;
            }
            Helper.SaveConfig();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }



        void SetIcon()
        {
            NotIcon.Icon = (Icon)rm.GetObject($"icon");
        }
    }


    public partial class WindowsTool
    {
       

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {

            StartUpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();


            if (_config.StartUp)
            {
                StartUpMenuItem.Checked = true;
            }
            StartUpMenuItem.CheckOnClick = true;
            //StartUpMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            StartUpMenuItem.Text = "开机启动";
            StartUpMenuItem.Click += new System.EventHandler(this.开机启动ToolStripMenuItem_Click);
       
            ExitMenuItem.Text = "退出";
            ExitMenuItem.Click += new System.EventHandler(this.退出ToolStripMenuItem_Click);

            contextMenu = new ContextMenuStrip(new Container());
            contextMenu.Items.AddRange(new ToolStripItem[] 
            {
                StartUpMenuItem,
                ExitMenuItem
            });
            contextMenu.Text = "桌面图标自动隐藏工具";


            NotIcon = new NotifyIcon()
            {
                //Icon = new Icon(AppDomain.CurrentDomain.BaseDirectory + "icons\\1.ico"),
                Icon = (Icon)rm.GetObject($"icon"),
                ContextMenuStrip = contextMenu,
                Text = "WindowsTop",
                Visible = true
            };
        }


 

        #endregion

        private NotifyIcon NotIcon { get; set; }
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem StartUpMenuItem;
        private ToolStripMenuItem ExitMenuItem;
    }



    public class Helper
    {
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

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

        /// <summary>
        /// 开机启动
        /// </summary>
        public static void StartUp()
        {
            //获取程序执行路径..
            string starupPath = AppDomain.CurrentDomain.BaseDirectory + Assembly.GetExecutingAssembly().GetName().Name + ".exe";
            //class Micosoft.Win32.RegistryKey. 表示Window注册表中项级节点,此类是注册表装.
            //RegistryKey loca = Registry.LocalMachine;
            RegistryKey loca = Registry.CurrentUser;
            RegistryKey run = loca.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

            try
            {
                //SetValue:存储值的名称
                run.SetValue("WindowsTopTool", starupPath);
                loca.Close();
            }
            catch (Exception ee)
            {
                //throw ee;
            }
        }

        /// <summary>
        /// 取消开机启动
        /// </summary>
        public static void CancelStartUp()
        {
            //获取程序执行路径..
            string starupPath = AppDomain.CurrentDomain.BaseDirectory + Assembly.GetExecutingAssembly().GetName().Name + ".exe";
            //class Micosoft.Win32.RegistryKey. 表示Window注册表中项级节点,此类是注册表装.
            //RegistryKey loca = Registry.LocalMachine;
            RegistryKey loca = Registry.CurrentUser;
            RegistryKey run = loca.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

            try
            {
                //SetValue:存储值的名称
                run.DeleteValue("WindowsTopTool");
                loca.Close();
            }
            catch (Exception ee)
            {
                //throw ee;
            }
        }



        static PerformanceCounter cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");

     
        private static string _configPath = AppDomain.CurrentDomain.BaseDirectory + "config.config";
        public static Config config;
        public static ResourceManager rm;
        public static void SaveConfig()
        {  
            Properties.Settings.Default.StartUp = config.StartUp;
            Properties.Settings.Default.Save();

        }

 
        public static void LoadConfig()
        {
            config = new Config()
            {
                StartUp = Properties.Settings.Default.StartUp,
            };
          
        }

    }

    [Serializable]
    public class Config
    {

        public static Config Instacnce = new Config();

        public bool StartUp { get; set; } = true;

    }
}










public partial class Form1 : Form
{
   
}

