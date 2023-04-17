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
using static DesktopIconTool.Helper;

namespace DesktopIconTool
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

            Console.Title = "DesktopIconTool";
            while(!ShowWindow(FindWindow(null, "DesktopIconTool"), 0)){

            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new IconTool());



            //隐藏控制台窗口
            //while (!ShowWindow(FindWindow(null, "DesktopIconTool"), 0))
            //{

            //}
            //Console.ReadLine();
            
            mutex.ReleaseMutex();
        }
          


        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        static bool SingleProcess()
        {
            mutex = new Mutex(true, "DesktopIconTool");
            if (!mutex.WaitOne(0, false))
            {
                return true;
            }
            return false;
        }

    }



    public partial class IconTool : ApplicationContext
    {

        ResourceManager rm;

        Config _config;
        public IconTool()
        {

            //string serverIP = ConfigurationManager.AppSettings["StartUp"];
            //string dataBase = ConfigurationManager.AppSettings["IsRun"];
            //string user = ConfigurationManager.AppSettings["HiddenToolBar"];
            //string password = ConfigurationManager.AppSettings["GifSpeed"];

            Helper.rm = rm = Resources.ResourceManager;
            //Helper.LoadConfig(rm.GetObject($"config").ToString());
            Helper.LoadConfig();
            _config = Helper.config;

            if (_config.StartUp)
            {
                Helper.StartUp();
            }

            InitializeComponent();
            MouseMoveTask();
            MousePointChanged += MousePointChange;
            SwitchIconTask();
            UpDateCpuUsageTask();
        }


        public static bool ShowDesktopIcon = false;
        public POINT CurrentMousePoint = new POINT(0, 0);
        void MouseMoveTask()
        {
            //50ms 获取记录一次鼠标位置， 若位置发生变化则通知时间
            Task.Run(() => {
                while (true)
                {
                    Helper.GetCursorPos(out POINT lpPoint);
                    if (lpPoint.X != CurrentMousePoint.X || lpPoint.Y != CurrentMousePoint.Y)
                    {
                        MousePointChanged();
                        CurrentMousePoint = lpPoint;
                    }
                    Thread.Sleep(50);
                }
            });


            Task.Run(() =>
            {

                while (true)
                {

                    if (curTimes >= Times)
                    {
                        Thread.Sleep(30);
                        continue;
                    }


                    while (curTimes++ < Times) //计时
                    {
                        Thread.Sleep(1000);
                    }
                    ShowDesktopIcon = false;
                    ShowHiddenIcon(false); //隐藏
                    if (_config.HiddenToolBar)
                    {
                        ShowTaskbar(false);
                    }
                    Thread.Sleep(30);
                }
            });
        }

        /// <summary>
        /// 鼠标未移动的时间
        /// </summary>
        int curTimes = 0;

        /// <summary>
        /// 
        /// </summary>
        readonly int Times = 10;

        /// <summary>
        /// 鼠标位置发生变化
        /// </summary>
        public void MousePointChange()
        {
            if (_config.IsRun)
            {
                curTimes = 0;
            }
            else
            {
                curTimes = int.MaxValue;
            }

            if (ShowDesktopIcon == false) //如果是隐藏则立马显现，如果是显示则不做操作
            {
                ShowDesktopIcon = true;
                ShowHiddenIcon(true);//显示桌面图标
                if (_config.HiddenToolBar) 
                {
                    ShowTaskbar(true); //显示任务栏
                }
            }
        }

        public Action MousePointChanged = delegate { };

        private static void ShowHiddenIcon(bool isShow)
        {

            // 遍历顶级窗口
            EnumWindows((hwnd, lParam) =>
            {
                // 找到第一个 WorkerW 窗口，此窗口中有子窗口 SHELLDLL_DefView，所以先找子窗口
                var shellDll = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellDll != IntPtr.Zero)
                {
                    // 找到当前第一个 WorkerW 窗口的，后一个窗口，及第二个 WorkerW 窗口。
                    //IntPtr tempHwnd = Win32Func.FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);

                    IntPtr Idesk1 = FindWindowEx(shellDll, IntPtr.Zero, "SysListView32", "FolderView"); //获取桌面 


                    //Win32Func.ShowWindow(Idesk, b % 5);  //value=5时显示，value=0时隐藏
                    int value = isShow ? 5 : 0;
                    ShowWindow(Idesk1, value);  //value=5时显示，value=0时隐藏
                }
                return true;
            }, IntPtr.Zero.ToInt32());

        }

        /// summary 
        /// 隐藏任务栏和桌面图标
        /// /summary 
        private static void ShowTaskbar(bool Taskbar)
        {
            IntPtr trayHwnd = FindWindow("Shell_TrayWnd", null);
            if (trayHwnd != IntPtr.Zero)
            {
                //ShowWindow(desktopPtr, );//隐藏桌面图标 （0是隐藏，1是显示）
                ShowWindow(trayHwnd, Taskbar ? 1 : 0);//隐藏任务栏（0是隐藏，1是显示）
                //ShowWindow(hStar, );//隐藏windows 按钮
            }
        }


        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = CharSet.Auto)]
        public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);


        public delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);
        [DllImport("user32.dll")]
        private static extern int EnumWindows(EnumWindowsCallback callPtr, int lParam);

        private void 运行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                _config.IsRun = true;
            }
            else
            {
                _config.IsRun = false;
            }
            Helper.SaveConfig();

        }

        private void 隐藏任务栏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                _config.HiddenToolBar = true;
            }
            else
            {
                _config.HiddenToolBar = false;
            }
            Helper.SaveConfig();
        }

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
            //this.Dispose();
            Application.Exit();
        }

        List<Icon> icons = new List<Icon>();

        void SwitchIconTask()
        {
            for (int i = 0; i < 18; i++)
            {
                //icons.Add(new Icon(AppDomain.CurrentDomain.BaseDirectory + "icons\\" + (i + 1) + ".ico"));
                icons.Add((Icon)rm.GetObject($"_{i + 1}"));
            }

            Task.Run(() =>
            {
                int i = 0;
                while (true)
                {
                    if (i >= icons.Count)
                    {
                        i = 0;
                    }
                    NotIcon.Icon = icons[i];
                    i++;
                    Thread.Sleep(IconRefreshTimeSpan);
                }
            });
        }

        int IconRefreshTimeSpan = 100; //ms
        private void Speend_Click(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)SpeedMenuItem.DropDownItems[0]).Checked = false;
            ((ToolStripMenuItem)SpeedMenuItem.DropDownItems[1]).Checked = false;
            ((ToolStripMenuItem)SpeedMenuItem.DropDownItems[2]).Checked = false;
            ((ToolStripMenuItem)SpeedMenuItem.DropDownItems[3]).Checked = false;
            var item = (System.Windows.Forms.ToolStripMenuItem)sender;
            item.Checked = true;
            if (item.Text == "起飞")
            {
                IconRefreshTimeSpan = 5;
                _config.GifSpeed = 3;
            }
            if (item.Text == "快")
            {
                IconRefreshTimeSpan = 35;
                _config.GifSpeed = 2;
            }
            if (item.Text == "正常")
            {
                IconRefreshTimeSpan = 100;
                _config.GifSpeed = 1;
            }
            if (item.Text == "慢")
            {
                IconRefreshTimeSpan = 200;
                _config.GifSpeed = 0;
            }
            Helper.SaveConfig();
        }


        void UpDateCpuUsageTask() 
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var cpuUsage = Helper.GetCpuUsage();
                    Console.Write(cpuUsage);
                    if (0<= cpuUsage && cpuUsage <= 10)
                    {
                        IconRefreshTimeSpan = 150;
                    }
                    else if (10 < cpuUsage && cpuUsage <= 20)
                    {
                        IconRefreshTimeSpan = 120;
                    }
                    else if (20 < cpuUsage && cpuUsage <= 30)
                    {
                        IconRefreshTimeSpan = 100;
                    }
                    else if (30 < cpuUsage && cpuUsage <= 40)
                    {
                        IconRefreshTimeSpan = 85;
                    }
                    else if (40 < cpuUsage && cpuUsage <= 50)
                    {
                        IconRefreshTimeSpan = 70;
                    }
                    else if (50 < cpuUsage && cpuUsage <= 60)
                    {
                        IconRefreshTimeSpan = 55;
                    }
                    else if (60 < cpuUsage && cpuUsage <= 70)
                    {
                        IconRefreshTimeSpan = 40;
                    }
                    else if (70 < cpuUsage && cpuUsage <= 80)
                    {
                        IconRefreshTimeSpan = 30;
                    }
                    else if (80 < cpuUsage && cpuUsage <= 90)
                    {
                        IconRefreshTimeSpan = 20;
                    }
                    else if (90 < cpuUsage && cpuUsage <= 100)
                    {
                        IconRefreshTimeSpan = 10;
                    }
                    else
                    {
                        IconRefreshTimeSpan = 5;
                    }

                    Thread.Sleep(1000);
                }
            });
        }
    }


    public partial class IconTool
    {
       

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {

            RunMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            StartUpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            HiddenTaskBarMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            SpeedMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            if (_config.IsRun)
            {
                RunMenuItem.Checked = true;
            }
            RunMenuItem.CheckOnClick = true;
            //RunMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            RunMenuItem.Text = "运行";
            RunMenuItem.Click += new System.EventHandler(this.运行ToolStripMenuItem_Click);

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

            SpeedMenuItem.Text = "速度";
            SpeedMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { 
                new System.Windows.Forms.ToolStripMenuItem()
                {
                    Text = "起飞",
                },
                new System.Windows.Forms.ToolStripMenuItem()
                {
                    Text = "快",
                },
                new System.Windows.Forms.ToolStripMenuItem()
                {
                    Text = "正常",

                    //Checked = true,
                },
                new System.Windows.Forms.ToolStripMenuItem()
                {
                    Text = "慢",
                },
            });
            SpeedMenuItem.DropDownItems[0].Click += Speend_Click; 
            SpeedMenuItem.DropDownItems[1].Click += Speend_Click; 
            SpeedMenuItem.DropDownItems[2].Click += Speend_Click; 
            SpeedMenuItem.DropDownItems[3].Click += Speend_Click;
            switch (_config.GifSpeed)
            {
                case 0: (SpeedMenuItem.DropDownItems[3] as ToolStripMenuItem).Checked = true; break;
                case 1: (SpeedMenuItem.DropDownItems[2] as ToolStripMenuItem).Checked = true; break;
                case 2: (SpeedMenuItem.DropDownItems[1] as ToolStripMenuItem).Checked = true; break;
                case 3: (SpeedMenuItem.DropDownItems[0] as ToolStripMenuItem).Checked = true; break;
            }


            if (_config.HiddenToolBar)
            {
                HiddenTaskBarMenuItem.Checked = true;
            }
            HiddenTaskBarMenuItem.CheckOnClick = true;
            HiddenTaskBarMenuItem.Text = "隐藏任务栏";
            HiddenTaskBarMenuItem.Click += new System.EventHandler(this.隐藏任务栏ToolStripMenuItem_Click);

            contextMenu = new ContextMenuStrip(new Container());
            contextMenu.Items.AddRange(new ToolStripItem[] 
            {
                SpeedMenuItem,
                HiddenTaskBarMenuItem,
                RunMenuItem,
                StartUpMenuItem,
                ExitMenuItem
            });
            contextMenu.Text = "桌面图标自动隐藏工具";


            NotIcon = new NotifyIcon()
            {
                //Icon = new Icon(AppDomain.CurrentDomain.BaseDirectory + "icons\\1.ico"),
                ContextMenuStrip = contextMenu,
                Text = "DesktopIconTool",
                Visible = true
            };
        }


 

        #endregion

        private NotifyIcon NotIcon { get; set; }
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem RunMenuItem;
        private ToolStripMenuItem StartUpMenuItem;
        private ToolStripMenuItem ExitMenuItem;
        private ToolStripMenuItem HiddenTaskBarMenuItem;
        private ToolStripMenuItem SpeedMenuItem;
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
                run.SetValue("DesktopIconHidden", starupPath);
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
                run.DeleteValue("DesktopIconHidden");
                loca.Close();
            }
            catch (Exception ee)
            {
                //throw ee;
            }
        }



        static PerformanceCounter cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");

        /// <summary>
        /// 获取CPU使用率
        /// </summary>
        /// <returns></returns>
        public static int GetCpuUsage()
        {
            //PerformanceCounter cpuCounter;
            //PerformanceCounter ramCounter;
            //cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            //cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
            //ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            //return ramCounter.NextValue() + "MB";
            return (int)cpuCounter.NextValue();
        }
     
        private static string _configPath = AppDomain.CurrentDomain.BaseDirectory + "config.config";
        public static Config config;
        public static ResourceManager rm;
        public static void SaveConfig()
        {
            //using (XmlTextWriter xw = new XmlTextWriter(_configPath, Encoding.Default))
            //{
            //    xw.Formatting = Formatting.Indented;
            //    xw.IndentChar = '\t';
            //    xw.Indentation = 1;
            //    try
            //    {
            //        XmlSerializer seriesr = new XmlSerializer(config.GetType());
            //        seriesr.Serialize(xw, config);
            //    }
            //    catch (Exception e)
            //    {
            //        throw e.InnerException;
            //    }
            //}


            /*
            Stream sm = new MemoryStream();
            using (XmlTextWriter xw = new XmlTextWriter(sm, Encoding.Default))
            {
                xw.Formatting = Formatting.Indented;
                xw.IndentChar = '\t';
                xw.Indentation = 1;
                try
                {
                    XmlSerializer seriesr = new XmlSerializer(config.GetType());
                    seriesr.Serialize(xw, config);
                }
                catch (Exception e)
                {
                    throw e.InnerException;
                }

                var value = StreamToStr(sm);
                UpdateResource(value);
            }
            */
         
            Properties.Settings.Default.StartUp = config.StartUp;
            Properties.Settings.Default.IsRun = config.IsRun;
            Properties.Settings.Default.HiddenToolBar = config.HiddenToolBar;
            Properties.Settings.Default.GifSpeed = config.GifSpeed;
            Properties.Settings.Default.Save();

        }

        static void UpdateResource(string value)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(rm.BaseName);
            XmlNodeList xnlist = xmlDoc.GetElementsByTagName("data");//这个data是固定
            foreach (XmlNode node in xnlist)
            {
                if (node.Attributes != null)
                {
                    if (node.Attributes["xml:space"].Value == "preserve")//这个preserve也是固定的
                    {
                        if (node.Attributes["name"].Value == "config")//String1是你想要编辑的
                        {
                            node.InnerText = value;//给他赋值就OK了
                        }
                    }
                }
            }
            xmlDoc.Save(rm.BaseName);//别忘记保存
        }

        public static void LoadConfig()
        {
            //try
            //{
            //    //rm.GetObject($"config");
            //    var serializer = new XmlSerializer(typeof(Config));
            //    //var fs = new FileStream(_configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            //    var stm = strToStream(configStr);
            //    config = (Config)serializer.Deserialize(stm);
            //    stm.Close();
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e);
            //    config = Config.Instacnce;
            //}

            config = new Config()
            {
                StartUp = Properties.Settings.Default.StartUp,
                IsRun = Properties.Settings.Default.IsRun,
                HiddenToolBar = Properties.Settings.Default.HiddenToolBar,
                GifSpeed = Properties.Settings.Default.GifSpeed
            };
          
        }

        static Stream strToStream(string xml)
        {
            //string test = “This is string″;

            // convert string to stream
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(xml);
            writer.Flush();

            stream.Position = 0;
            //or 
            //stream.Seek(0, SeekOrigin.Begin);
            return stream;

            //// convert stream to string
            //stream.Position = 0;
            //StreamReader reader = new StreamReader(stream);
            //string text = reader.ReadToEnd();
        }

        static string StreamToStr(Stream stream)
        {
            //// convert stream to string
            stream.Position = 0;
            StreamReader reader = new StreamReader(stream);
            string xmlStr = reader.ReadToEnd();
            return xmlStr;
        }

    }

    [Serializable]
    public class Config
    {

        public static Config Instacnce = new Config();

        public bool StartUp { get; set; } = true;

        public bool IsRun { get; set; } = true;

        public bool HiddenToolBar { get; set; } = false;

        public int GifSpeed { get; set; } = 1;
    }
}
