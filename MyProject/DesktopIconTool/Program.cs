using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        public IconTool()
        {

            InitializeComponent();

            // Create a resource manager. 
            rm = Resources.ResourceManager;

            Helper.StartUp();
            MouseMoveTask();
            MousePointChanged += MousePointChange;
            SwitchIconTask();
        }


        public static bool ShowDesktopIcon = false;
        public POINT CurrentMousePoint = new POINT(0, 0);
        void MouseMoveTask()
        {
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


            Task t = new Task(() =>
            {

                while (true)
                {
                    if (curTimes >= Times)
                    {
                        continue;
                    }
                    while (curTimes++ < Times) //计时
                    {
                        Thread.Sleep(1000);
                    }
                    ShowDesktopIcon = false;
                    ShowHiddenIcon(ShowDesktopIcon); //隐藏
                    if (IsHiddenTaskBar)
                    {
                        ShowTaskbar(ShowDesktopIcon);
                    }
                    Thread.Sleep(30);
                }

            });
            t.Start();
        }

        int curTimes = 0;
        readonly int Times = 10;
        bool AlwaysShowIcon = false;
        bool IsHiddenTaskBar = false;

        public void MousePointChange()
        {
            if (AlwaysShowIcon)
            {
                curTimes = 9999999;
            }
            else
            {
                curTimes = 0;
            }
            if (ShowDesktopIcon == false) //如果是隐藏则立马显现，如果是显示则不做操作
            {
                ShowDesktopIcon = true;
                ShowHiddenIcon(ShowDesktopIcon);//显示
                if (IsHiddenTaskBar)
                {
                    ShowTaskbar(ShowDesktopIcon);
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
                AlwaysShowIcon = false;

            }
            else
            {
                AlwaysShowIcon = true;

            }
        }


        private void 隐藏任务栏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                IsHiddenTaskBar = true;
            }
            else
            {
                IsHiddenTaskBar = false;
            }
        }

        private void 开机启动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                Helper.StartUp();
            }
            else
            {
                Helper.CancelStartUp();
            }
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
            }
            if (item.Text == "快")
            {
                IconRefreshTimeSpan = 35;
            }
            if (item.Text == "正常")
            {
                IconRefreshTimeSpan = 100;
            }
            if (item.Text == "慢")
            {
                IconRefreshTimeSpan = 200;
            }
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

            RunMenuItem.Checked = true;
            RunMenuItem.CheckOnClick = true;
            RunMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            RunMenuItem.Text = "运行";
            RunMenuItem.Click += new System.EventHandler(this.运行ToolStripMenuItem_Click);
       
            StartUpMenuItem.Checked = true;
            StartUpMenuItem.CheckOnClick = true;
            StartUpMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
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
                    Checked = true,
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



        public static void StartUp()
        {
            //获取程序执行路径..
            string starupPath = AppDomain.CurrentDomain.BaseDirectory + "DesktopIcon.exe";
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


        public static void CancelStartUp()
        {
            //获取程序执行路径..
            string starupPath = AppDomain.CurrentDomain.BaseDirectory + "DesktopIcon.exe";
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

    }
}
