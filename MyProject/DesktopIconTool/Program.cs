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
using DesktopIconTool.Helper;

namespace DesktopIconTool
{
    class Program
    {

        [STAThread]
        static void Main(string[] args)
        {

            //IsHasWindowActive();

            //单例程序
            if (ProgramTool.SingleProcess("DesktopIconTool"))
                return;

            Console.Title = "DesktopIconTool";
            while(!Win32Helper.ShowWindow(Win32Helper.FindWindow(null, "DesktopIconTool"), 0)){

            }

            if (Properties.Settings.Default.StartUp)
            {
                ProgramTool.StartUp();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new IconTool());


            ProgramTool.ReleaseSingleProcess();
        }
  

    }



    public partial class IconTool : ApplicationContext
    {

        public IconTool()
        {
       
            InitializeComponent();
            MouseMoveTask();
            AutoHiddenDesktopIconAndTaskBarThread();
            SwitchIconTask();
            UpDateCpuUsageTask();
            MousePointChanged += MousePointChange;
        }


        public static bool ShowDesktopIcon = false;
        public POINT CurrentMousePoint = new POINT(0, 0); //记录当前鼠标的位置
        int curTimes = 0; //鼠标未移动的时间计数
        readonly int Times = 10; //定义鼠标未移动多少秒后隐藏图标
        public Action MousePointChanged = delegate { };
        List<Icon> icons = new List<Icon>(); //循环替换程序Icon，实现动图效果
        int IconRefreshTimeSpan = 100; //刷新图标的间隔，ms 

        /// <summary>
        ///监控鼠标位置 线程
        /// </summary>
        void MouseMoveTask()
        {
            //50ms 获取记录一次鼠标位置， 若位置发生变化则通知时间
            Task.Run(() => {
                while (true)
                {
                    Win32Helper.GetCursorPos(out POINT lpPoint);
                    if (lpPoint.X != CurrentMousePoint.X || lpPoint.Y != CurrentMousePoint.Y)
                    {
                        MousePointChanged();
                        CurrentMousePoint = lpPoint;
                    }
                    Thread.Sleep(50);
                }
            });
        }

        /// <summary>
        /// 自动隐藏桌面图标和任务栏 线程
        /// </summary>
        void AutoHiddenDesktopIconAndTaskBarThread()
        {
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
                    if (Properties.Settings.Default.HiddenToolBar)
                    {
                        if (!Win32Helper.IsHasWindowActive())
                        {
                            ShowTaskbar(false);
                        }
                    }
                    Thread.Sleep(30);
                }
            });
        }

        /// <summary>
        /// 鼠标位置发生变化
        /// </summary>
        public void MousePointChange()
        {
            if (Properties.Settings.Default.IsRun)
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
                if (Properties.Settings.Default.HiddenToolBar) 
                {
                    ShowTaskbar(true); //显示任务栏
                }
            }
        }

        /// <summary>
        /// 显示或隐藏图标
        /// </summary>
        /// <param name="isShow"></param>
        private static void ShowHiddenIcon(bool isShow)
        {

            // 遍历顶级窗口
            Win32Helper.EnumWindows((hwnd, lParam) =>
            {
                // 找到第一个 WorkerW 窗口，此窗口中有子窗口 SHELLDLL_DefView，所以先找子窗口
                var shellDll = Win32Helper.FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellDll != IntPtr.Zero)
                {
                    // 找到当前第一个 WorkerW 窗口的，后一个窗口，及第二个 WorkerW 窗口。
                    //IntPtr tempHwnd = Win32Func.FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);

                    IntPtr Idesk1 = Win32Helper.FindWindowEx(shellDll, IntPtr.Zero, "SysListView32", "FolderView"); //获取桌面 


                    //Win32Func.ShowWindow(Idesk, b % 5);  //value=5时显示，value=0时隐藏
                    int value = isShow ? 5 : 0;
                    Win32Helper.ShowWindow(Idesk1, value);  //value=5时显示，value=0时隐藏
                }
                return true;
            }, IntPtr.Zero);

        }

        /// <summary>
        /// 隐藏任务栏和桌面图标
        /// </summary>
        /// <param name="Taskbar"></param>
        private static void ShowTaskbar(bool Taskbar)
        {
            IntPtr trayHwnd = Win32Helper.FindWindow("Shell_TrayWnd", null);
            if (trayHwnd != IntPtr.Zero)
            {
                //ShowWindow(desktopPtr, );//隐藏桌面图标 （0是隐藏，1是显示）
                Win32Helper.ShowWindow(trayHwnd, Taskbar ? 1 : 0);//隐藏任务栏（0是隐藏，1是显示）
                //ShowWindow(hStar, );//隐藏windows 按钮
            }
        }

   
        private void 运行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                Properties.Settings.Default.IsRun = true;
            }
            else
            {
                Properties.Settings.Default.IsRun = false;
            }
            Properties.Settings.Default.Save();

        }

        private void 插件管理ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(PluginManageWin.Instacne == null)
            {
                PluginManageWin.CreateInstance();
                PluginManageWin.Instacne.Show();
            }
            else
            {
                PluginManageWin.Instacne.Activate();
            }
        }

        private void 隐藏任务栏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                Properties.Settings.Default.HiddenToolBar = true;
            }
            else
            {
                Properties.Settings.Default.HiddenToolBar = false;
            }
            Properties.Settings.Default.Save();

        }

        private void 开机启动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                ProgramTool.StartUp();
                Properties.Settings.Default.StartUp = true;
            }
            else
            {
                ProgramTool.CancelStartUp();
                Properties.Settings.Default.StartUp = false;
            }
            Properties.Settings.Default.Save();

        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //this.Dispose();
            NotIcon.Dispose(); //释放notifyIcon1的所有资源，以保证托盘图标在程序关闭时立即消失
            Application.Exit();
        }


        /// <summary>
        /// 循环icon实现动态icon 线程
        /// </summary>
        void SwitchIconTask()
        {
            for (int i = 0; i < 18; i++)
            {
                //icons.Add(new Icon(AppDomain.CurrentDomain.BaseDirectory + "icons\\" + (i + 1) + ".ico"));
                icons.Add((Icon)Resources.ResourceManager.GetObject($"_{i + 1}"));
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
                Properties.Settings.Default.GifSpeed = 3;
            }
            if (item.Text == "快")
            {
                IconRefreshTimeSpan = 35;
                Properties.Settings.Default.GifSpeed = 2;
            }
            if (item.Text == "正常")
            {
                IconRefreshTimeSpan = 100;
                Properties.Settings.Default.GifSpeed = 1;
            }
            if (item.Text == "慢")
            {
                IconRefreshTimeSpan = 200;
                Properties.Settings.Default.GifSpeed = 0;
            }
            Properties.Settings.Default.Save();

        }

        /// <summary>
        /// 刷新获取CPU使用率 线程
        /// </summary>
        void UpDateCpuUsageTask() 
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var cpuUsage = ProgramTool.GetCpuUsage();
                    NotIcon.Text = $"cpu:{cpuUsage.ToString()}%";
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
        private NotifyIcon NotIcon { get; set; }
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem PluginMangageMenuItem;
        private ToolStripMenuItem PluginsMenuItem;
        private ToolStripMenuItem RunMenuItem;
        private ToolStripMenuItem StartUpMenuItem;
        private ToolStripMenuItem ExitMenuItem;
        private ToolStripMenuItem HiddenTaskBarMenuItem;
        private ToolStripMenuItem SpeedMenuItem;

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {

            PluginMangageMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            RunMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            StartUpMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            HiddenTaskBarMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            SpeedMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            if (Properties.Settings.Default.IsRun)
            {
                RunMenuItem.Checked = true;
            }
            RunMenuItem.CheckOnClick = true;
            //RunMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            RunMenuItem.Text = "运行";
            RunMenuItem.Click += new System.EventHandler(this.运行ToolStripMenuItem_Click);

            if (Properties.Settings.Default.StartUp)
            {
                StartUpMenuItem.Checked = true;
            }
            StartUpMenuItem.CheckOnClick = true;
            //StartUpMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            StartUpMenuItem.Text = "开机启动";
            StartUpMenuItem.Click += new System.EventHandler(this.开机启动ToolStripMenuItem_Click);
       
            ExitMenuItem.Text = "退出";
            ExitMenuItem.Click += new System.EventHandler(this.退出ToolStripMenuItem_Click);

            PluginMangageMenuItem.Text = "插件管理";
            PluginMangageMenuItem.Click += new System.EventHandler(this.插件管理ToolStripMenuItem_Click);

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
            switch (Properties.Settings.Default.GifSpeed)
            {
                case 0: (SpeedMenuItem.DropDownItems[3] as ToolStripMenuItem).Checked = true; break;
                case 1: (SpeedMenuItem.DropDownItems[2] as ToolStripMenuItem).Checked = true; break;
                case 2: (SpeedMenuItem.DropDownItems[1] as ToolStripMenuItem).Checked = true; break;
                case 3: (SpeedMenuItem.DropDownItems[0] as ToolStripMenuItem).Checked = true; break;
            }


            if (Properties.Settings.Default.HiddenToolBar)
            {
                HiddenTaskBarMenuItem.Checked = true;
            }
            HiddenTaskBarMenuItem.CheckOnClick = true;
            HiddenTaskBarMenuItem.Text = "隐藏任务栏";
            HiddenTaskBarMenuItem.Click += new System.EventHandler(this.隐藏任务栏ToolStripMenuItem_Click);

            contextMenu = new ContextMenuStrip(new Container());
            contextMenu.Items.AddRange(new ToolStripItem[] 
            {
                PluginMangageMenuItem,
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

      
    }


   
}

