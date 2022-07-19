using AutomationServices.EmguCv;
using AutomationServices.EmguCv.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestNuget
{
    class QQSignIn
    {

        public static Process SimulatorProcess;

        public static string BaseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
        public static void Start()
        {

            string simulatorPath = @"E:\Program Files\Microvirt\MEmu\MEmu.exe";
            //E:\Program Files\Microvirt\MEmu\MEmu.exe
            //if (!Helper.GetSoftWare("逍遥模拟器", out simulatorPath))
            //return;
            //SimulatorProcess = Process.Start(simulatorPath);//启动模拟器
            //Thread.Sleep(10000);


            string pic = BaseDirectory + "Images\\QQApp.jpg";
            WaitFindAndClick(pic);
            var WindowRect = WindowHelper.GetWindowLocationSize(SimulatorProcess.MainWindowHandle);
            Thread.Sleep(10000);

            string LoginAgain = BaseDirectory + "Images\\ReLogin.jpg";
            bool isLogin = WaitFindAndClick(LoginAgain, ClickLocation.RightBottom, new Point(-50, -10), new MatchOptions(10, 1000) { Threshold = 0.99 });

            if (!isLogin)
            {
                string login = BaseDirectory + "Images\\login.jpg";
                WaitFindAndClick(login, ClickLocation.LeftTop, new Point(10, 10), new MatchOptions() { Threshold = 0.99 });
                WaitFindAndClick(login);
            }
            Thread.Sleep(10000);

            string head = BaseDirectory + "Images\\头像.jpg";
            WaitFindAndClick(head, ClickLocation.LeftCenter);
            Thread.Sleep(10000);

            string Punch = BaseDirectory + "Images\\打卡.jpg";
            WaitFindAndClick(Punch, ClickLocation.CenterTop);
            Thread.Sleep(5000);

            string PunchConfirm = BaseDirectory + "Images\\立即打卡.jpg";
            var matchOptions = new MatchOptions();
            matchOptions.MaxTimes = 10;
            matchOptions.DelayInterval = 1000;
            matchOptions.DelayInterval = 1000;
            matchOptions.Threshold = 0.99;
            matchOptions.MatchMode = MatchMode.Absolutely;
            matchOptions.WindowArea = WindowRect;
            matchOptions.ImreadModesConvert = ImreadModesConvert.Color;
            WaitFindAndClick(PunchConfirm, default, default, matchOptions);
            Thread.Sleep(1500);
            KeyBoardHelper.KeyDownUp(Keys.F5); //模拟器返回快捷键

            Thread.Sleep(2000);
            string ViewLevel = BaseDirectory + "Images\\QQ等级.png";
            WaitFindAndClick(ViewLevel);
            Thread.Sleep(5000);

            Thread.Sleep(20000);
            SimulatorProcess.Kill();
            Console.ReadLine();
        }


        public static bool WaitFindAndClick(string Picture, ClickLocation ClickLocation = ClickLocation.Center, Point Offset = default, MatchOptions MatOptions = null)
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
                    var point = ImageHelper.GetClickPoint(rct, MatOptions, ClickLocation, Offset );
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
    }



   
}
