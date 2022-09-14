using AutomationServices.EmguCv;
using AutomationServices.EmguCv.Helper;
using Emgu.CV.CvEnum;
using Emgu.CV.ML;
using Emgu.CV.Ocl;
using QQSpeed_SmartApp.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Window = System.Windows.Window;

namespace QQSpeed_SmartApp
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string BaseDirectory = System.AppDomain.CurrentDomain.BaseDirectory + "Images\\";

        public Process QQSpeedProcess;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {

            //Win32Helper.SetWindowNotBorder(QQSpeedProcess.MainWindowHandle);
            StartUpQQSpeed();
            Login();
            InitQQSpeedWindow();
            if (petFlatCheck.IsChecked == true)
            {
                PetFight();
            }
            if (GhostWorldCheck.IsChecked == true)
            {
                GhostWorld();
            }
            if (FleetCheck.IsChecked == true)
            {
                Fleet();
            }
            if(ExitCheck.IsChecked == true)
            {
              QQSpeedProcess.Kill();
            }
        }

        private void excute_Click(object sender, RoutedEventArgs e)
        {
            if (petFlatCheck.IsChecked == true)
            {
                PetFight();
            }
            if (GhostWorldCheck.IsChecked == true)
            {
                GhostWorld();
            }
            if (FleetCheck.IsChecked == true)
            {
                Fleet();
            }
            if (ExitCheck.IsChecked == true)
            {
                QQSpeedProcess.Kill();
            }
        }
        
        /// <summary>
        /// 启动QQ飞车
        /// </summary>
        void StartUpQQSpeed()
        {
            //WinExec(@"W:\Program Files\腾讯游戏\QQ飞车\Releasephysx27\QQSpeed_Launch.exe", 1);

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = @"W:\Program Files\腾讯游戏\QQ飞车\Releasephysx27\QQSpeed_Launch.exe";
            info.Arguments = "";
            info.WindowStyle = ProcessWindowStyle.Minimized;
            Process pro = Process.Start(info);
            pro.WaitForInputIdle();
            Thread.Sleep(2000);
            string pName = "QQSpeed_loader";//要启动的进程名称，可以在任务管理器里查看，一般是不带.exe后缀的;
            Process[] temp = Process.GetProcessesByName(pName);//在所有已启动的进程中查找需要的进程；
            foreach (var p in temp)
            {
                Win32Helper.OpenAndSetWindow(p.MainWindowHandle);
            }
        }

        /// <summary>
        /// 登录
        /// </summary>
        void Login()
        {
            Thread.Sleep(1000);
            MyHelper.Click(645, 565, 300);
            MyHelper.Click(645, 660, 300);
            MyHelper.Click(540, 635, 300);

            var accountInput = new System.Drawing.Point(450, 423);
            MouseHelper.MouseDownUp(accountInput.X, accountInput.Y);
            MyHelper.StrInputKey("193589375");
            Thread.Sleep(500);
            var pwdInput = new System.Drawing.Point(450, 460);
            MouseHelper.MouseDownUp(pwdInput.X, pwdInput.Y);
            MyHelper.StrInputKey("yr18723750041..");

            var loginBtn = new System.Drawing.Point(750, 435);
            MouseHelper.MouseDownUp(loginBtn.X, loginBtn.Y);


        }

        /// <summary>
        /// 初始化窗口
        /// </summary>
        void InitQQSpeedWindow()
        {
            string pName = "QQSpeedCefProcess";//要启动的进程名称，可以在任务管理器里查看，一般是不带.exe后缀的;
            Process[] temp;
            while (true)
            {
                temp = Process.GetProcessesByName(pName);//在所有已启动的进程中查找需要的进程；
                if (temp.Length > 0)
                {
                    break;
                }
                Thread.Sleep(1000);
            }
            pName = "GameApp";
            Process[] temp2;
            while (true)
            {
                temp2 = Process.GetProcessesByName(pName);//在所有已启动的进程中查找需要的进程；
                if (temp.Length > 0)
                {
                    break;
                }
                Thread.Sleep(1000);
            }

            //Win32Helper.SetWindowNotBorder(temp2[0].MainWindowHandle);

            //Win32Helper.OpenAndSetWindow(temp2[0].MainWindowHandle,1280,768);
            QQSpeedProcess = temp2[0];
            Win32Helper.OpenAndSetWindow(QQSpeedProcess.MainWindowHandle, 1026, 800, -1, -32);
            var WindowRect = WindowHelper.GetWindowLocationSize(QQSpeedProcess.MainWindowHandle);
            Thread.Sleep(15000);
            BackInitPage();
        }

        /// <summary>
        /// 宠物对战
        /// </summary>
        void PetFight()
        {
            MouseHelper.MouseDownUp(1001, 483); //宠物
            Thread.Sleep(1000);
            MouseHelper.MouseDownUp(449, 179); //进入宠物界面
            Thread.Sleep(1000);
            MouseHelper.MouseDownUp(649, 682); // 天梯
            Thread.Sleep(1000);
            MouseHelper.MouseDownUp(706, 610); //每日奖励
            Thread.Sleep(1000);
            MouseHelper.MouseDownUp(511, 499); //立即领取
            Thread.Sleep(1000);
            MouseHelper.MouseDownUp(511, 499); //确认
            Thread.Sleep(1000);

            for (int i = 0; i < 6; i++)
            {
                MouseHelper.MouseDownUp(410, 600); // 开始匹配
                string image1 = BaseDirectory + "对战币.png";

                var matchOptions = new MatchOptions();
                matchOptions.MaxTimes = 0;
                matchOptions.DelayInterval = 2000;
                matchOptions.Threshold = 0.99;
                matchOptions.MatchMode = MatchMode.Absolutely;
                //matchOptions.WindowArea = WindowHelper.GetWindowLocationSize(QQSpeedProcess.MainWindowHandle);
                matchOptions.ImreadModesConvert = ImreadModesConvert.Color;
                MyHelper.WaitFind(image1, matchOptions);
                MouseHelper.MouseDownUp(511, 499); //确定
                Thread.Sleep(1000);
            }

            //膜拜
            MouseHelper.MouseDownUp(574, 214);
            Thread.Sleep(300);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            Thread.Sleep(3300);
            MouseHelper.MouseDownUp(574, 241);
            Thread.Sleep(300);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            Thread.Sleep(300);
            MouseHelper.MouseDownUp(574, 262);
            Thread.Sleep(300);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            Thread.Sleep(300);
            MouseHelper.MouseDownUp(574, 294);
            Thread.Sleep(300);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            Thread.Sleep(300);
            MouseHelper.MouseDownUp(574, 321);
            Thread.Sleep(300);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            Thread.Sleep(300);
            KeyBoardHelper.KeyDownUp(Keys.Escape);

            //悬赏
            MouseHelper.MouseDownUp(487, 683);
            Thread.Sleep(500);
            MouseHelper.MouseDownUp(520, 560); //一键挑战
            Thread.Sleep(300);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            Thread.Sleep(300);


            for (int i = 0; i < 5 ; i++)
            {
                MouseHelper.MouseDownUp(700, 560); //特别悬赏
                Thread.Sleep(300);
                MouseHelper.MouseDownUp(510, 610); //挑战
                Thread.Sleep(300);
                string image = BaseDirectory + "对战币.png";
                var matOptions = new MatchOptions();
                matOptions.MaxTimes = 0;
                matOptions.DelayInterval = 2000;
                matOptions.Threshold = 0.99;
                matOptions.MatchMode = MatchMode.Absolutely;
                //matchOptions.WindowArea = WindowHelper.GetWindowLocationSize(QQSpeedProcess.MainWindowHandle);
                matOptions.ImreadModesConvert = ImreadModesConvert.Color;
                MyHelper.WaitFind(image, matOptions);
                KeyBoardHelper.KeyDownUp(Keys.Enter);
                Thread.Sleep(100);
            }


            Thread.Sleep(1000);
            MouseHelper.MouseDownUp(565, 690);
            Thread.Sleep(300);
            MouseHelper.MouseDownUp(380, 490); //快速挑战
            Thread.Sleep(7000);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(380, 490); //快速挑战
            Thread.Sleep(7000);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(380, 490); //快速挑战
            Thread.Sleep(7000);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(380, 490); //快速挑战
            Thread.Sleep(7000);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(380, 490); //快速挑战
            Thread.Sleep(7000);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(380, 490); //快速挑战
            Thread.Sleep(7000);
            KeyBoardHelper.KeyDownUp(Keys.Enter);

            MouseHelper.MouseDownUp(380, 490); //快速挑战
            Thread.Sleep(200);
            MouseHelper.MouseDownUp(450, 490); //确定
            Thread.Sleep(7000);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(380, 490); //快速挑战
            Thread.Sleep(200);
            MouseHelper.MouseDownUp(450, 490); //确定
            Thread.Sleep(7000);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(380, 490); //快速挑战
            Thread.Sleep(200);
            MouseHelper.MouseDownUp(450, 490); //确定
            Thread.Sleep(7000);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(380, 490); //快速挑战
            Thread.Sleep(200);
            MouseHelper.MouseDownUp(450, 490); //确定
            Thread.Sleep(7000);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(380, 490); //快速挑战
            Thread.Sleep(200);
            MouseHelper.MouseDownUp(450, 490); //确定
            Thread.Sleep(7000);
            KeyBoardHelper.KeyDownUp(Keys.Enter);

            MouseHelper.MouseDownUp(435, 140); //箱子
            Thread.Sleep(200);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(536, 140); //箱子
            Thread.Sleep(200);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(638, 140); //箱子
            Thread.Sleep(200);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(746, 140); //箱子
            Thread.Sleep(200);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(846, 140); //箱子
            Thread.Sleep(200);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(500, 215); //箱子B
            Thread.Sleep(200);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(670, 215); //箱子B
            Thread.Sleep(200);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            MouseHelper.MouseDownUp(830, 215); //箱子B
            Thread.Sleep(200);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            Thread.Sleep(200);

            MouseHelper.MouseDownUp(1000, 15);


        }
        void BackInitPage()
        {
            MouseHelper.MouseDownUp(1000, 15);
            for (int i = 0; i < 10; i++)
            {
                KeyBoardHelper.KeyDownUp(Keys.Back);
                KeyBoardHelper.KeyDownUp(Keys.Escape);
                Thread.Sleep(1000);
            }
            MouseHelper.MouseDownUp(70, 35);
            Thread.Sleep(1000);
            KeyBoardHelper.KeyDownUp(Keys.Back);

        }

        void GhostWorld()
        {

            BackInitPage();
            MouseHelper.MouseDownUp(1001, 483); //宠物
            Thread.Sleep(1000);
            MouseHelper.MouseDownUp(555, 150); //我的精灵
            Thread.Sleep(300);
            MouseHelper.MouseDownUp(430, 340); //精灵世界
            Thread.Sleep(2000);
            //爱抚精灵 5次
            for (int i = 0; i < 5; i++)
            {
                MouseHelper.MouseDownUp(625, 530); //点击精灵
                Thread.Sleep(100);
                MouseHelper.MouseDownUp(650, 500);
                Thread.Sleep(300);
            }
            MouseHelper.MouseDownUp(625, 530);
            Thread.Sleep(300);
            MouseHelper.MouseDownUp(570, 500); //喂养
            Thread.Sleep(300);
            MouseHelper.MouseDownUp(650, 450); //蓝莓
            Thread.Sleep(1000);


            MouseHelper.MouseMove(200, 240); //精灵工坊
            Thread.Sleep(1000);
            MouseHelper.MouseDownUp(); //精灵工坊
            Thread.Sleep(1000);
            MouseHelper.MouseDownUp(); //精灵工坊
            Thread.Sleep(2500);
            MouseHelper.MouseDownUp(750, 620); //领取奖励
            Thread.Sleep(500);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            Thread.Sleep(500);
            MouseHelper.MouseDownUp(340, 323); //添加工作
            Thread.Sleep(500);
            MouseHelper.MouseDownUp(650, 350); //修理机器
            Thread.Sleep(100);
            MouseHelper.MouseDownUp(510, 460);
            Thread.Sleep(300);
            MouseHelper.MouseDownUp(750, 620); //立即开始
            Thread.Sleep(300);
            KeyBoardHelper.KeyDownUp(Keys.Enter);
            Thread.Sleep(300);
            MouseHelper.MouseDownUp(863, 110); //×
            Thread.Sleep(300);

            MouseHelper.MouseDownUp(160, 420); //许愿树
            Thread.Sleep(2500);
            for (int i = 0; i < 3; i++)
            {
                MouseHelper.MouseDownUp(530, 210); //浇水
                Thread.Sleep(1000);
            }
      

            for (int i = 0; i < 3; i++)
            {
                MouseHelper.MouseDownUp(530, 425); //祈福
                Thread.Sleep(200);
                MouseHelper.MouseDownUp(670, 435); //确定
                Thread.Sleep(1000);
            }
            Thread.Sleep(300);

            for (int i = 0; i < 10; i++)
            {
                if(i == 9)
                {
                    MouseHelper.MouseDownUp(880, 580); //下一页
                    Thread.Sleep(300);
                    MouseHelper.MouseDownUp(888, 180); //拜访
                    Thread.Sleep(1500);
                    MouseHelper.MouseDownUp(115, 440); //浇水
                    Thread.Sleep(200);
                    KeyBoardHelper.KeyDownUp(Keys.Enter); //确定
                    Thread.Sleep(200);
                }
                else
                {
                    int j = i;
                    MouseHelper.MouseDownUp(888, 180 + j * 40); //拜访
                    Thread.Sleep(1500);
                    MouseHelper.MouseDownUp(115, 440); //浇水
                    Thread.Sleep(200);
                    KeyBoardHelper.KeyDownUp(Keys.Enter); //确定
                    Thread.Sleep(200);
                }
            }
            Thread.Sleep(300);
        }

        /// <summary>
        /// 车队
        /// </summary>
        void Fleet()
        {
            BackInitPage();
            MyHelper.Click(1000, 400, 2000); //信息
            MyHelper.Click(25, 410, 300); //车队
            MyHelper.Click(355, 240, 300); //进入车队
            MyHelper.Click(420, 160, 1000); //福利
            MyHelper.Click(420, 160, 500); //福利
            MyHelper.Click(180, 470, 500); //点击领取
            
            MyHelper.KeyPress(Keys.Enter, 300);
            MyHelper.Click(650, 340, 500); //福袋
            MyHelper.Click(390, 485, 500); //领取
            MyHelper.KeyPress(Keys.Enter, 300);
            MyHelper.Click(530, 155, 500); //抢红包

            for (int i = 0; i < 5; i++)
            {
                MyHelper.Click(670, 310 + i * 40, 500); //车队红包
                MyHelper.KeyPress(Keys.Enter, 2500);
            }

            MyHelper.Click(430, 155, 500); //发红包
            MyHelper.Click(625, 487, 500); //发红包
            MyHelper.KeyPress(Keys.Enter, 300);
            Thread.Sleep(100);

        }

        private void Paotu_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => { 
                Thread.Sleep(3000);
                for (int i = 0; i < 10; i++)
                {
                    KeyBoardHelper.KeyDown(Keys.W);
                    Thread.Sleep(10_000);
                    KeyBoardHelper.KeyDownUp(Keys.R);
                    KeyBoardHelper.KeyUp(Keys.W);
                }
            });
        }

        private void 祝我好运_Click(object sender, RoutedEventArgs e)
        {
            StartUpQQSpeed();
            Login();
            InitQQSpeedWindow();
            //金丝篓
            Action action = () => {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    MyHelper.Click(995, 235, 2000);
                    MyHelper.Click(405, 170, 2000);
                    MyHelper.Click(85, 420, 500); //捕捉
                });
            };
            new ScheduledTask().StartExecuteTask(23, 59, 00 , action);

            Action action2 = () => {
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    MyHelper.Click(455, 485, 2000); //确定
                    for (int i = 0; i < 4; i++)
                    {
                        MyHelper.Click(565, 520, 2000); //继续开启
                    }
                    Thread.Sleep(10000);
                    QQSpeedProcess.Kill();
                });
            };
            new ScheduledTask().StartExecuteTask(23, 59, 56, action2);
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
                KeyBoardHelper.KeyDownUp(StringToKey(key));
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
