using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QQSpeed_SmartApp.Helper
{

    /// <summary>
    /// 定时任务类
    /// </summary>
    class ScheduledTask
    {

        public List<TimerObject> TimerList = new List<TimerObject>();
        public void StartExecuteTask(double hour, double min, double Sec, Action execute)
        {
            TimerList.Add(CreateDailyScheduledTask(hour, min, Sec, execute));
        }


        private TimerObject CreateDailyScheduledTask(double hour, double min, double Sec,Action execute)
        {
            Thread.Sleep(50);

            DateTime now = DateTime.Now;
            DateTime oneOClock = DateTime.Today.AddHours(hour).AddMinutes(min).AddSeconds(Sec);
            if (now > oneOClock)
                oneOClock = oneOClock.AddDays(1.0);

            var timeState = new TimeState()
            {
                Hour = hour,
                Minutes = min,
                Seconds = Sec,
                TimeID = now,
                action = execute,
            };
            int waitTime = (int)((oneOClock - now).TotalMilliseconds);
            TimerCallback timerDelegate = new TimerCallback(StartScheduledTask);
            var t = new Timer(timerDelegate, timeState, waitTime, Timeout.Infinite);
            TimerObject timerObject = new TimerObject()
            {
                TimeID = now,
                sTimer = t,
            };
            return timerObject;
        }

        //要执行的任务
        private async void StartScheduledTask(object state)
        {
            var timeState = state as TimeState;

            //执行功能...
            //AutomatedSelenium selenium = new AutomatedSelenium();
            //selenium.StartTask();
            await Task.Run(timeState.action);

            //再次设定
            TimerList.Add(CreateDailyScheduledTask(timeState.Hour, timeState.Minutes, timeState.Seconds, timeState.action));

            var timerObj = TimerList.Where(t => t.TimeID == timeState.TimeID).FirstOrDefault();
            if (timerObj != null)
                TimerList.Remove(timerObj);
        }
    }


    public class TimeState
    {
        public double Hour { get; set; }
        public double Minutes { get; set; }
        public double Seconds { get; set; }

        public DateTime TimeID { get; set; }

        public Action action { get; set; }
    }

    public class TimerObject
    {
        public DateTime TimeID { get; set; }

        public Timer sTimer { get; set; }

    }
}
