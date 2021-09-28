using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    class Program
    {
        public static List<int> array = new List<int>();
        static void Main(string[] args)
        {


            Task t1 = new Task(() =>
            {

                lock (array)
                {
                    while (array.Count<20)
                    {
                        array.Add(array.Count);
                        Console.WriteLine("Add: " + array.Count);
                        Thread.Sleep(500);
                    }
                }
            });
            t1.Start();

            Task t2 = new Task(() =>
            {
                lock (array)
                {
                    while (true)
                    {
                        if (array.Count != 0)
                        {
                            array.RemoveAt(array.Count - 1);
                            Console.WriteLine("delete: " + array.Count);
                        }
                        Thread.Sleep(1000);
                    }
                }
            });
            t2.Start();
            Console.ReadLine();
        }


    }
}
