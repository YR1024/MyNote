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
using static DesktopIconTool.Helper.TaskbarStyle;
using ScheduleReminder;


namespace DesktopIconTool
{
    class Program
    {

        [STAThread]
        static void Main(string[] args)
        {

            // ================= 新增：全局异常捕获 =================
            // 设置捕获UI线程的异常
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            // 处理UI线程异常
            Application.ThreadException += Application_ThreadException;
            // 处理非UI线程异常（如 Task, Thread）
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            // 处理未观察到的Task异常
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            // ======================================================

            //Win32Helper.IsHasWindowActive();

            //单例程序
            if (ProgramTool.SingleProcess("DesktopIconTool"))
                return;

            Console.Title = "DesktopIconTool";
#if !DEBUG
            while (!Win32Helper.ShowWindow(Win32Helper.FindWindow(null, "DesktopIconTool"), 0))
            {
            }
#endif

            if (Properties.Settings.Default.StartUp)
            {
                ProgramTool.StartUp();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new IconTool());


            ProgramTool.ReleaseSingleProcess();
        }

        // --- 以下为新增的异常处理和日志写入方法 ---

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            WriteCrashLog("UI线程异常 (ThreadException)", e.Exception);
            Logger.Info(e.Exception.ToString());
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteCrashLog("非UI线程异常 (UnhandledException)", e.ExceptionObject as Exception);
            Logger.Info((e.ExceptionObject as Exception).ToString());

        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            WriteCrashLog("Task内部异常 (UnobservedTaskException)", e.Exception);
            Logger.Info(e.Exception.ToString());
            e.SetObserved(); // 标记异常已观察，防止程序因此崩溃
        }

        private static void WriteCrashLog(string exceptionType, Exception ex)
        {
            try
            {
                if (ex == null) return;

                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string logFile = Path.Combine(logDir, $"CrashLog_{DateTime.Now:yyyyMMdd}.txt");

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("=========================================================");
                sb.AppendLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                sb.AppendLine($"类型: {exceptionType}");
                sb.AppendLine($"异常信息: {ex.Message}");
                sb.AppendLine($"堆栈跟踪:\r\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    sb.AppendLine($"内部异常: {ex.InnerException.Message}");
                    sb.AppendLine($"内部堆栈:\r\n{ex.InnerException.StackTrace}");
                }
                sb.AppendLine("=========================================================\r\n");

                File.AppendAllText(logFile, sb.ToString());
            }
            catch
            {
                // 日志写入失败时忽略，防止引发新的崩溃
            }
        }
    }



    public partial class IconTool : ApplicationContext
    {
        IScheduleReminder scheduleReminder;
        public IconTool()
        {
            try
            {
                scheduleReminder = new Schedule();
                scheduleReminder.Start();
                InitializeComponent();
                AutoHiddenDesktopIconAndTaskBarThread();
                SwitchIconTask();
                UpDateCpuUsageTask();

                //MouseMoveTask(); //改为Hook方式
                HookTool.MouseMoved += MouseMoveEvent;
                //HookTool.MouseLeftButtonDown += MouseLeftButtonDown;
                HookTool.KeyDown += KeyDown;
                HookTool.Start();
                if (Properties.Settings.Default.KeyboardHookEnabled)
                {
                    HookTool.StartKeyboardHook();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
           
        }

        private bool KeyDown(Keys keys, int downup)
        {
            Console.WriteLine("按键：" + keys);
            //if (keys == Keys.Escape)
            if (keys == Keys.LControlKey)
            {

                var hwnd = Win32Helper.GetForegroundWindow();
                if (hwnd != IntPtr.Zero)
                {
                    StringBuilder title = new StringBuilder(MaxWindowTitleLength);
                    Win32Helper.GetWindowText(hwnd, title, MaxWindowTitleLength);

                    if (string.IsNullOrEmpty(title.ToString()))
                    {
                        string processName = Win32Helper.GetProcessNameFromHandle(hwnd);
                        var Exstyle = Win32Helper.GetWindowLong(hwnd, -20);
                        if (processName == "QQ" && Exstyle == 4295491976) //视频播放
                        {
                            Win32Helper.ResizeWindow(hwnd, 300, 300);
                            return true;
                        }
                    }
                    else if (title.ToString().Contains("的聊天记录"))
                    {
                        Win32Helper.SendMessage(hwnd, 0x0010, IntPtr.Zero, IntPtr.Zero);
                        return true;
                    }
                }
                return false;
            }
            return false;
        }

        //private async void MouseLeftButtonDown(int x, int y)
        //{
        //    await Task.Run(async () => {
        //        await Task.Delay(1000);
        //        var h = Win32Helper.GetForegroundWindow();
        //        Console.WriteLine("窗口句柄：" + h);
        //        Console.WriteLine("窗口句柄INT：" + h.ToInt32());
        //    });
          
        //}

        private async void MouseMoveEvent(int x, int y)
        {
            //Console.WriteLine("鼠标位置：" + x + " " + y);
            await MousePointChange();
            //CurrentMousePoint = new POINT(x, y);
        }

        protected override void Dispose(bool disposing)
        {
            HookTool.Stop();
            base.Dispose(disposing);
        }



        public static bool ShowDesktopIcon = false;
        //public POINT CurrentMousePoint = new POINT(0, 0); //记录当前鼠标的位置
        int curTimes = 0; //鼠标未移动的时间计数
        readonly int Times = 10; //定义鼠标未移动多少秒后隐藏图标
        List<Icon> icons = new List<Icon>(); //循环替换程序Icon，实现动图效果
        int IconRefreshTimeSpan = 100; //刷新图标的间隔，ms 
        const int MaxWindowTitleLength = 256;


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
        public async Task MousePointChange()
        {
            await Task.Run(() =>
            {
                if (Properties.Settings.Default.IsRun)
                    curTimes = 0;
                else
                    curTimes = int.MaxValue;

                if (ShowDesktopIcon == false) //如果是隐藏则立马显现，如果是显示则不做操作
                {
                    ShowDesktopIcon = true;
                    ShowHiddenIcon(true);//显示桌面图标
                    if (Properties.Settings.Default.HiddenToolBar)
                    {
                        ShowTaskbar(true); //显示任务栏
                    }
                }
            });
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



        private void 键盘钩子ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                Properties.Settings.Default.KeyboardHookEnabled = true;
            }
            else
            {
                Properties.Settings.Default.KeyboardHookEnabled = false;
            }
            Properties.Settings.Default.Save();

            if (Properties.Settings.Default.KeyboardHookEnabled)
            {
                HookTool.StartKeyboardHook();
            }
            else
            {
                HookTool.StopKeyboardHook();
            }
        }

        private void 日程提醒ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            scheduleReminder.ShowWindow();
        }


        private void 任务栏透明ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ((ToolStripMenuItem)TaskBarTransparentMenuItem.DropDownItems[0]).Checked = false;
            ((ToolStripMenuItem)TaskBarTransparentMenuItem.DropDownItems[1]).Checked = false;
            ((ToolStripMenuItem)TaskBarTransparentMenuItem.DropDownItems[2]).Checked = false;
            var item = (System.Windows.Forms.ToolStripMenuItem)sender;
            item.Checked = true;
            if (item.Text == "默认")
            {
                Properties.Settings.Default.TransparentTaskBar = 0;
                TaskbarStyle.SetTaskbarTransparency(AccentState.ACCENT_DISABLED);
            }
            if (item.Text == "透明")
            {
                Properties.Settings.Default.TransparentTaskBar = 1;
                TaskbarStyle.SetTaskbarTransparency(AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT);
                TaskbarStyle.SetTaskbarTransparency(AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND);
            }
            if (item.Text == "亚克力")
            {
                Properties.Settings.Default.TransparentTaskBar = 2;
                TaskbarStyle.SetTaskbarTransparency(AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND);
                TaskbarStyle.SetTaskbarTransparency(AccentState.ACCENT_ENABLE_BLURBEHIND);
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
                    //Console.Write(cpuUsage);
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
        private ToolStripMenuItem TaskBarTransparentMenuItem;
        private ToolStripMenuItem ExtensionMenuItem;

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
            TaskBarTransparentMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ExtensionMenuItem = new System.Windows.Forms.ToolStripMenuItem();

            {
                if (Properties.Settings.Default.IsRun)
                {
                    RunMenuItem.Checked = true;
                }
                RunMenuItem.CheckOnClick = true;
                //RunMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
                RunMenuItem.Text = "运行";
                RunMenuItem.Click += new System.EventHandler(this.运行ToolStripMenuItem_Click);
            }


            {
                if (Properties.Settings.Default.StartUp)
                {
                    StartUpMenuItem.Checked = true;
                }
                StartUpMenuItem.CheckOnClick = true;
                //StartUpMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
                StartUpMenuItem.Text = "开机启动";
                StartUpMenuItem.Click += new System.EventHandler(this.开机启动ToolStripMenuItem_Click);
            }

            {
                ExitMenuItem.Text = "退出";
                ExitMenuItem.Click += new System.EventHandler(this.退出ToolStripMenuItem_Click);
            }

            {
                PluginMangageMenuItem.Text = "插件管理";
                PluginMangageMenuItem.Click += new System.EventHandler(this.插件管理ToolStripMenuItem_Click);
            }        


            {
                if (Properties.Settings.Default.HiddenToolBar)
                {
                    HiddenTaskBarMenuItem.Checked = true;
                }
                HiddenTaskBarMenuItem.CheckOnClick = true;
                HiddenTaskBarMenuItem.Text = "隐藏任务栏";
                HiddenTaskBarMenuItem.Click += new System.EventHandler(this.隐藏任务栏ToolStripMenuItem_Click);
            }

            {
                TaskBarTransparentMenuItem.Text = "任务栏透明";
                TaskBarTransparentMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                    new System.Windows.Forms.ToolStripMenuItem()
                    {
                        Text = "默认",
                    },
                    new System.Windows.Forms.ToolStripMenuItem()
                    {
                        Text = "透明",
                    },
                    new System.Windows.Forms.ToolStripMenuItem()
                    {
                        Text = "亚克力",
                    },
                });
                TaskBarTransparentMenuItem.DropDownItems[0].Click += 任务栏透明ToolStripMenuItem_Click;
                TaskBarTransparentMenuItem.DropDownItems[1].Click += 任务栏透明ToolStripMenuItem_Click;
                TaskBarTransparentMenuItem.DropDownItems[2].Click += 任务栏透明ToolStripMenuItem_Click;
                switch (Properties.Settings.Default.TransparentTaskBar)
                {
                    case 0: (TaskBarTransparentMenuItem.DropDownItems[0] as ToolStripMenuItem).Checked = true; break;
                    case 1: (TaskBarTransparentMenuItem.DropDownItems[1] as ToolStripMenuItem).Checked = true; break;
                    case 2: (TaskBarTransparentMenuItem.DropDownItems[2] as ToolStripMenuItem).Checked = true; break;
                }
            }

            //扩展
            {
                ExtensionMenuItem.Text = "扩展";
                ToolStripMenuItem qqMenuItem = new System.Windows.Forms.ToolStripMenuItem("键盘钩子(QQ聊天辅助)");
                ExtensionMenuItem.DropDownItems.Add(qqMenuItem);
                if (Properties.Settings.Default.KeyboardHookEnabled)
                {
                    qqMenuItem.Checked = true;
                }
                qqMenuItem.CheckOnClick = true;
                qqMenuItem.Click += new System.EventHandler(this.键盘钩子ToolStripMenuItem_Click);

                ToolStripMenuItem srMenuItem = new System.Windows.Forms.ToolStripMenuItem("日程提醒");
                ExtensionMenuItem.DropDownItems.Add(srMenuItem);
                srMenuItem.Click += new System.EventHandler(this.日程提醒ToolStripMenuItem_Click);


            }


            contextMenu = new ContextMenuStrip(new Container());
            contextMenu.Items.AddRange(new ToolStripItem[] 
            {
                ExtensionMenuItem,
                PluginMangageMenuItem,
                HiddenTaskBarMenuItem,
                TaskBarTransparentMenuItem,
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

