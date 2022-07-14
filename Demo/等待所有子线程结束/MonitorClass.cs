using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace 等待所有子线程结束
{
    public class MonitorClass
    {
        int _ThreadCount = 5;
        int finishcount = 0;
        object locker = new object();
        public void Main()
        {
            for (int i = 0; i < _ThreadCount; i++)
            {
                Thread trd = new Thread(new ParameterizedThreadStart(ThreadMethod));
                trd.Start(i);
            }
            lock (locker)
            {
                while (finishcount != _ThreadCount)
                {
                    Monitor.Wait(locker);//等待
                }
            }
            Console.WriteLine("Thread Finished!");
        }

        private void ThreadMethod(object obj)
        {
            //模拟执行程序
            Thread.Sleep(3000);
            Console.WriteLine("Thread execute at {0}", obj.ToString());
            lock (locker)
            {
                finishcount++;
                Monitor.Pulse(locker); //完成，通知等待队列,告知已完，执行下一个。
            }
        }
    }
}
