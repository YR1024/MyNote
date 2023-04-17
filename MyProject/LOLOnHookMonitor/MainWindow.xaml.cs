using AutomationServices.EmguCv;
using AutomationServices.EmguCv.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LOLOnHookMonitor
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            //FindWindowIntPtrs();
            InitializeComponent();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            MouseHelper.ScreenWidth = 1440;
            MouseHelper.ScreenHeight = 900;
            LOLProcessMonitorTask();
            ClientProcessMonitorTask();
            GameProcessMonitorTask();
        }

        void LOLProcessMonitorTask()
        {        
            Task.Run(() =>
            {
                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        StartUpLOL();
                        Login();
                        Thread.Sleep(15000);
                        StartLolHunter();
                    }));
                }
                catch (Exception ex)
                {
                    AddLog(ex.Message);
                }
            });
        }

        bool ClientProcessMonitorTaskFlag = false;
        void ClientProcessMonitorTask()
        {
            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        ClientProcessMonitorTaskFlag = true;
                        Process[] LOLClientProcesses = Process.GetProcessesByName("LeagueClient");
                        if (LOLClientProcesses.Length >= 1)
                        {
                            InfoBox.Dispatcher.Invoke(() => {
                                InfoBox.Text += DateTime.Now.ToString() + "\n";
                                InfoBox.Text += $"当前处于客户端程序中...，\n";
                                InfoBox.ScrollToEnd();
                            });
                        }
                        else
                        {
                            int waitTime = 0;
                            while (waitTime < 300)
                            {
                                Process[] ClientProcesses = Process.GetProcessesByName("LeagueClient");
                                if (ClientProcesses.Length == 0)
                                {
                                    InfoBox.Dispatcher.Invoke(() => {
                                        InfoBox.Text += DateTime.Now.ToString() + "\n";
                                        InfoBox.Text += $"当前不在客户端程序中...，\n";
                                        InfoBox.ScrollToEnd();
                                    });
                                    waitTime += 30;
                                }
                                else
                                {
                                    break;
                                }
                                Thread.Sleep(30_000);
                            }
                            if (waitTime >= 300)
                            {
                                InfoBox.Dispatcher.Invoke(() => {
                                    InfoBox.Text += DateTime.Now.ToString() + "\n";
                                    InfoBox.Text += $"超过5分钟没有处于客户端中。。\n";
                                    InfoBox.ScrollToEnd();
                                });
                                ReLoadTask();
                            }
                        }
                        Task.Delay(30000).Wait();
                    }
                }
                catch (Exception e)
                {
                    InfoBox.Dispatcher.Invoke(() => {
                        InfoBox.Text += $"\n\n异常：{e.Message}" + "\n";
                        InfoBox.ScrollToEnd();
                    });
                }
                finally
                {
                    ClientProcessMonitorTaskFlag = false;
                    ReLoadTask();
                }
            });
        }

        bool GameProcessMonitorTaskFlag = false;
        void GameProcessMonitorTask()
        {
            Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        GameProcessMonitorTaskFlag = true;
                        Process[] LOLGameProcesses = Process.GetProcessesByName("League of Legends");
                        if (LOLGameProcesses.Length >= 1)
                        {
                            int waitTime = 0;
                            while (waitTime < 3000) //50min
                            {
                                Process[] GameProcesses = Process.GetProcessesByName("League of Legends");
                                if (GameProcesses.Length >= 1)
                                {
                                    InfoBox.Dispatcher.Invoke(() => {
                                        InfoBox.Text += DateTime.Now.ToString() + "\n";
                                        InfoBox.Text += $"正在游戏中...，\n";
                                        InfoBox.ScrollToEnd();
                                    });
                                    waitTime += 30;
                                }
                                else
                                {
                                    break;
                                }
                                Thread.Sleep(30_000);
                            }
                            if (waitTime >= 3000)
                            {
                                InfoBox.Dispatcher.Invoke(() => {
                                    InfoBox.Text += DateTime.Now.ToString() + "\n";
                                    InfoBox.Text += $"处于游戏中超过50分钟...\n";
                                    InfoBox.ScrollToEnd();
                                });
                                ReLoadTask();
                            }
                        }
                        else
                        {
                            int waitTime = 0;
                            while (waitTime < 600)
                            {
                                Process[] GameProcesses = Process.GetProcessesByName("League of Legends");
                                if (GameProcesses.Length == 0)
                                {
                                    InfoBox.Dispatcher.Invoke(() => {
                                        InfoBox.Text += DateTime.Now.ToString() + "\n";
                                        InfoBox.Text += $"当前不在游戏中...，\n";
                                        InfoBox.ScrollToEnd();
                                    });
                                    waitTime += 30;
                                }
                                else
                                {
                                    break;
                                }
                                Thread.Sleep(30_000);
                            }
                            if(waitTime >= 600)
                            {
                                InfoBox.Dispatcher.Invoke(() => {
                                    InfoBox.Text += DateTime.Now.ToString() + "\n";
                                    InfoBox.Text += $"超过十分钟没有处于游戏中。。\n";
                                    InfoBox.ScrollToEnd();
                                });
                                ReLoadTask();
                            }
                        }
                        Task.Delay(30000).Wait();
                    }
                }
                catch (Exception e)
                {
                    InfoBox.Dispatcher.Invoke(() => {
                        InfoBox.Text += $"\n\n异常：{e.Message}" + "\n";
                        InfoBox.ScrollToEnd();
                    });
                }
                finally
                {
                    GameProcessMonitorTaskFlag = false;
                    ReLoadTask();
                }
            });          
        }


        void ReLoadTask()
        {
            try
            {

                InfoBox.Dispatcher.Invoke(() => {
                    InfoBox.Text += DateTime.Now.ToString() + "\n";
                    InfoBox.Text += $"重新启动...\n";
                    InfoBox.ScrollToEnd();
                });
                Process[] LoginProcesses = Process.GetProcessesByName("Client");
                Process[] Clientprocesses = Process.GetProcessesByName("LeagueClient");
                Process[] Gameprocesses = Process.GetProcessesByName("League of Legends");
                foreach (var p in LoginProcesses)
                {
                    p.Kill();
                }
                foreach (var p in Clientprocesses)
                {
                    p.Kill();
                }
                foreach (var p in Gameprocesses)
                {
                    p.Kill();
                }


                LOLProcessMonitorTask();
                if (!ClientProcessMonitorTaskFlag)
                {
                    ClientProcessMonitorTask();
                }
                if (!GameProcessMonitorTaskFlag)
                {
                    GameProcessMonitorTask();
                }
            }
            catch (Exception e)
            {
                InfoBox.Text += e.Message;
            }
        }

        /// <summary>
        /// 启动LOL
        /// </summary>
        void StartUpLOL()
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = path.Text;
            info.Arguments = "";
            info.WindowStyle = ProcessWindowStyle.Minimized;
            Process pro = Process.Start(info);
            AddLog("启动LOL");
            Thread.Sleep(20_000);

            string pName = "Client";//要启动的进程名称，可以在任务管理器里查看，一般是不带.exe后缀的;
            Process[] temp = Process.GetProcessesByName(pName);//在所有已启动的进程中查找需要的进程；
            foreach (var p in temp)
            {
                Win32Helper.OpenAndSetWindow(p.MainWindowHandle,1280,768);
                AddLog("移动窗口");
            }
        }
     
        /// <summary>
        /// 登录
        /// </summary>
        void Login()
        {
            Thread.Sleep(5000);
            MyHelper.Click(1140, 320, 2000); //qq登录
            AddLog("qq登录");
            MyHelper.Click(1140, 320, 2000); //qq登录
            AddLog("qq登录");

            //MyHelper.Click(1090, 312, 300);
            //MyHelper.StrInputKey(accountTb.Text);

            //MyHelper.Click(1090, 380, 300);
            //MyHelper.StrInputKey(passwordTb.Password);

            //MyHelper.Click(1150, 575, 300); //登录

            MyHelper.Click(1160, 615, 5000); //快速登录
            AddLog("快速登录");
            MyHelper.Click(1150, 560, 20000); //登录
            AddLog("登录");
            MyHelper.Click(910, 635, 1000); //确认区服
            AddLog("确认区服");
        }

        void StartLolHunter()
        {
            var lolHunterIntPtr = Win32Helper.FindWindow(null, "LOLHunter - 亨特儿");
            var ctrlIntPtrs = Win32Helper.EnumChildWindowsCallback(lolHunterIntPtr);

            if (ctrlIntPtrs.Count == 0)
            {
                AddLog("子控件句柄数量0");
            }
            var startBtnIp = ctrlIntPtrs.Where(i => i.szClassName == "WindowsForms10.Button.app.0.33c0d9d_r3_ad1").LastOrDefault();

            if (startBtnIp.hWnd == IntPtr.Zero)
            {
                AddLog("没找到开始按钮句柄");
                return;
            }
            //const int WM_CLICK = 0x00F5;
            //Win32Helper.SendMessage(startBtnIp.hWnd, WM_CLICK, IntPtr.Zero, IntPtr.Zero);

            //var startBtnIp2 = ctrlIntPtrs.Where(i => i.szWindowName == "启动").LastOrDefault();

            if (startBtnIp.szWindowName == "启动")
            {
                Win32Helper.SendClick(startBtnIp.hWnd);
                Thread.Sleep(1500);
                Win32Helper.SendClick(startBtnIp.hWnd);
            }
        }

        void FindWindowIntPtrs()
        {
            var lolHunterIntPtr = Win32Helper.FindWindow(null, "英雄联盟登录程序");
            var ctrlIntPtrs = Win32Helper.EnumChildWindowsCallback(lolHunterIntPtr);
            var ctrlIntPtrs2 = Win32Helper.EnumChildWindowsCallback(ctrlIntPtrs[1].hWnd);

            Process[] temp = Process.GetProcessesByName("Client");
            //var startBtnIp = ctrlIntPtrs.Where(i => i.szClassName == "WindowsForms10.Button.app.0.33c0d9d_r3_ad1").LastOrDefault();

            //if (startBtnIp.hWnd == IntPtr.Zero)
            //    return;
            ////const int WM_CLICK = 0x00F5;
            ////Win32Helper.SendMessage(startBtnIp.hWnd, WM_CLICK, IntPtr.Zero, IntPtr.Zero);

            ////var startBtnIp2 = ctrlIntPtrs.Where(i => i.szWindowName == "启动").LastOrDefault();

            //if (startBtnIp.szWindowName == "启动")
            //{
            //    Win32Helper.SendClick(startBtnIp.hWnd);
            //    Thread.Sleep(1500);
            //    Win32Helper.SendClick(startBtnIp.hWnd);
            //}
        }


        void AddLog(string info)
        {
            InfoBox.Dispatcher.Invoke(new Action(() =>{
                InfoBox.Text += $"{info}\n";
            }));
            
        }
    }


    public class MyHelper
    {
        public static bool WaitFindAndClick(string Picture, ClickLocation ClickLocation = ClickLocation.Center, System.Drawing.Point Offset = default, MatchOptions MatOptions = null)
        {
            if (MatOptions == null)
                MatOptions = new MatchOptions();
            int executeTimes = 0;
            while (true)
            {
                var rct = EmguCvHelper.GetMatchPos(Picture, out double Similarity, MatOptions);
                if (rct != System.Drawing.Rectangle.Empty)
                {
                    Console.WriteLine("找到图片" + Picture);
                    Console.WriteLine("相似度" + Similarity);
                    var point = ImageHelper.GetClickPoint(rct, MatOptions, ClickLocation, Offset);
                    MouseHelper.MouseDownUp(point.X, point.Y);
                    return true;
                }
                executeTimes++;
                if (executeTimes >= MatOptions.MaxTimes && MatOptions.MaxTimes != 0)
                {
                    Console.WriteLine("在限定次数内没有找到图片" + Picture + "相似度：" + Similarity);
                    return false;
                }
                if (MatOptions.DelayInterval != 0)
                    Thread.Sleep(MatOptions.DelayInterval);
            }
        }

        public static bool WaitFind(string Picture, out System.Drawing.Rectangle rectangle, MatchOptions MatOptions = null)
        {
            if (MatOptions == null)
                MatOptions = new MatchOptions();
            int executeTimes = 0;
            while (true)
            {
                rectangle = EmguCvHelper.GetMatchPos(Picture, out double Similarity, MatOptions);
                if (rectangle != System.Drawing.Rectangle.Empty)
                {
                    //Console.WriteLine("找到图片" + Picture);
                    //Console.WriteLine("相似度" + Similarity);
                    return true;
                }
                executeTimes++;
                if (executeTimes >= MatOptions.MaxTimes && MatOptions.MaxTimes != 0)
                {
                    //Console.WriteLine("在限定次数内没有找到图片" + Picture + "相似度：" + Similarity);
                    return false;
                }
                //if (MatOptions.DelayInterval != 0)
                Thread.Sleep(MatOptions.DelayInterval);
            }
        }

        public static bool WaitFind(string Picture, MatchOptions MatOptions = null)
        {
            if (MatOptions == null)
                MatOptions = new MatchOptions();
            int executeTimes = 0;
            while (true)
            {
                var rct = EmguCvHelper.GetMatchPos(Picture, out double Similarity, MatOptions);
                if (rct != System.Drawing.Rectangle.Empty)
                {
                    Console.WriteLine("找到图片" + Picture);
                    Console.WriteLine("相似度" + Similarity);
                    return true;
                }
                executeTimes++;
                if (executeTimes >= MatOptions.MaxTimes && MatOptions.MaxTimes != 0)
                {
                    Console.WriteLine("在限定次数内没有找到图片" + Picture + "相似度：" + Similarity);
                    return false;
                }
                if (MatOptions.DelayInterval != 0)
                    Thread.Sleep(MatOptions.DelayInterval);
            }
        }

        public static void StrInputKey(string keyStr)
        {
            foreach (var key in keyStr)
            {
                //KeyBoardHelper.KeyDownUp(StringToKey(key));
                WinIoHelper.KeyDown(StringToKey(key));
                Thread.Sleep(50);
            }
        }

        public static System.Windows.Forms.Keys StringToKey(char key)
        {
            switch (key)
            {
                case '0': return Keys.D0;
                case '1': return Keys.D1;
                case '2': return Keys.D2;
                case '3': return Keys.D3;
                case '4': return Keys.D4;
                case '5': return Keys.D5;
                case '6': return Keys.D6;
                case '7': return Keys.D7;
                case '8': return Keys.D8;
                case '9': return Keys.D9;
                case 'y': return Keys.Y;
                case 'r': return Keys.R;
                case '.': return Keys.Decimal;
                default: return Keys.None;
            }
        }


        public static void Click(int x, int y, int time)
        {
            MouseHelper.MouseDownUp(x, y);
            Thread.Sleep(time);
        }

        public static void KeyPress(Keys keys, int time)
        {
            KeyBoardHelper.KeyDownUp(keys);
            Thread.Sleep(time);
        }
    }


}
