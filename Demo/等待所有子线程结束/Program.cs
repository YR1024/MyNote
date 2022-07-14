using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace 等待所有子线程结束
{
    class Program
    {
        static void Main(string[] args)
        {
            /////////// Test 1
            //for (int i = 0; i < 5; i++)
            //{
            //    ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadMethod), i);
            //}
            //int maxWorkerThreads, workerThreads;
            //int portThreads;
            //while (true)
            //{
            //    /*
            //     GetAvailableThreads()：检索由 GetMaxThreads 返回的线程池线程的最大数目和当前活动数目之间的差值。
            //     而GetMaxThreads 检索可以同时处于活动状态的线程池请求的数目。
            //     通过最大数目减可用数目就可以得到当前活动线程的数目，如果为零，那就说明没有活动线程，说明所有线程运行完毕。
            //     */
            //    ThreadPool.GetMaxThreads(out maxWorkerThreads, out portThreads);
            //    ThreadPool.GetAvailableThreads(out workerThreads, out portThreads);
            //    if (maxWorkerThreads - workerThreads == 0)
            //    {
            //        Console.WriteLine("Thread Finished!");
            //        break;
            //    }
            //}


            /////////// Test 2
            MonitorClass test2 = new MonitorClass();
            test2.Main();



            Console.ReadLine();
        }
        static List<int> b;

        private static void ThreadMethod(object i)
        {
            //模拟程序运行
            Thread.Sleep((new Random().Next(1, 4)) * 1000);
            Console.WriteLine("Thread execute at {0}", i.ToString());
        }
    }
}
