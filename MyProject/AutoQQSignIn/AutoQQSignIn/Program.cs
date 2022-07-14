using Emgu.CV.CvEnum;
using Services;
using Services.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoQQSignIn
{
    class Program
    {
        static void Main(string[] args)
        {
            //隐藏控制台窗口,TEST为控制台名称
            Console.Title = "QQSignInConsole";
            new ScheduledTask().StartExecuteTask();
            ShowWindow(FindWindow(null, "QQSignInConsole"), 0);
            Console.ReadLine();
        }



        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    }

    /// <summary>
    /// 定时任务类
    /// </summary>
    class ScheduledTask
    {

        public List<TimerObject> TimerList = new List<TimerObject>();
        public void StartExecuteTask()
        {
            TimerList.Add(CreateDailyScheduledTask(0, 0));
        }


        private TimerObject CreateDailyScheduledTask(int hour, double min)
        {
            Thread.Sleep(50);

            double time = hour + (min / 60f);
            DateTime now = DateTime.Now;
            DateTime oneOClock = DateTime.Today.AddHours(time);
            if (now > oneOClock)
                oneOClock = oneOClock.AddDays(1.0);

            var timeState = new TimeState()
            {
                SetTime = time,
                TimeID = now,
            };
            int waitTime = (int)((oneOClock - now).TotalMilliseconds);
            TimerCallback timerDelegate = new TimerCallback(StartScheduledTask);
            var t = new System.Threading.Timer(timerDelegate, timeState, waitTime, Timeout.Infinite);
            TimerObject timerObject = new TimerObject()
            {
                TimeID = now,
                sTimer = t,
            };
            return timerObject;
        }

        //要执行的任务
        private void StartScheduledTask(object state)
        {
            //执行功能...
            QQSignIn.Start();

            //再次设定
            var timeState = state as TimeState;
            //var time = Convert.ToDouble(state);
            int hour = (int)timeState.SetTime;
            var min = Convert.ToInt32((timeState.SetTime - hour) * 60);
            TimerList.Add(CreateDailyScheduledTask(hour, min));

            var timerObj = TimerList.Where(t => t.TimeID == timeState.TimeID).FirstOrDefault();
            if (timerObj != null)
                TimerList.Remove(timerObj);
        }
    }


    public class TimeState
    {
        public double SetTime { get; set; }
        public DateTime TimeID { get; set; }
    }
    public class TimerObject
    {
        public DateTime TimeID { get; set; }

        public System.Threading.Timer sTimer { get; set; }

    }
}
