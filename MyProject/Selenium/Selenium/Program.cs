using Microsoft.ML.Data;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;

namespace Selenium
{
    class Program
    {
        private static Mutex mutex;
        static void Main(string[] args)
        {
            //Test();
            //return;



#if DEBUG
            //立即执行 Debug
            AutomatedSelenium selenium = new AutomatedSelenium()
            {
                ShowBrowserWnd = true,
            };
            selenium.StartTask();
#else

            //单例程序
            if (SingleProcess())
                return;
            //定时任务
            new ScheduledTask().StartExecuteTask();
            //开机启动
            StartUp(); 
#endif

            //隐藏控制台窗口
            Console.Title = "QQ农场牧场自动化";
            ShowWindow(FindWindow(null, "QQ农场牧场自动化"), 0);

            Console.ReadLine();
        }


        public static void Test()
        {
            // Create single instance of sample data from first line of dataset for model input.
            var image = MLImage.CreateFromFile(@"D:\360MoveData\Users\YR\Desktop\SliderVerificationCode\Bg\26.png");
            Slider.ModelInput sampleData = new Slider.ModelInput()
            {
                Image = image,
            };
            // Make a single prediction on the sample data and print results.
            var predictionResult = Slider.Predict(sampleData);
            Console.WriteLine("\n\nPredicted Boxes:\n");
            if (predictionResult.PredictedBoundingBoxes == null)
            {
                Console.WriteLine("No Predicted Bounding Boxes");
                return;
            }
            var boxes =
                predictionResult.PredictedBoundingBoxes.Chunk(4)
                    .Select(x => new { XTop = x[0], YTop = x[1], XBottom = x[2], YBottom = x[3] })
                    .Zip(predictionResult.Score, (a, b) => new { Box = a, Score = b });

            foreach (var item in boxes)
            {
                Console.WriteLine($"XTop: {item.Box.XTop},YTop: {item.Box.YTop},XBottom: {item.Box.XBottom},YBottom: {item.Box.YBottom}, Score: {item.Score}");
            }

            return;

        }

  

        static bool SingleProcess()
        {
            mutex = new Mutex(true, "MySeleniumConsole");
            if (!mutex.WaitOne(0, false))
            {
                return true;
            }
            return false;
        }

        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //static void Main(string[] args)
        //{
        //    Console.Title = "SeleniumConsole";
        //    IntPtr intptr = FindWindow("ConsoleWindowClass", "SeleniumConsole");
        //    if (intptr != IntPtr.Zero)
        //    {
        //        ShowWindow(intptr, 0);//隐藏这个窗⼝
        //    }
        //    string x;
        //    x = Console.ReadLine();
        //}



        //[DllImport("User32.dll")]
        //public static extern int ShowWindow(int hwnd, int nCmdShow);
        //[DllImport("User32.dll")]
        //public static extern int FindWindow(string lpClassName, string lpWindowName);
        //private const int SW_HIDE = 0;
        //private const int SW_NORMAL = 1;
        //private const int SW_MAXIMIZE = 3;
        //private const int SW_SHOWNOACTIVATE = 4;
        //private const int SW_SHOW = 5;
        //private const int SW_MINIMIZE = 6;
        //private const int SW_RESTORE = 9;
        //private const int SW_SHOWDEFAULT = 10;


        private static void StartUp()
        {
            //获取程序执行路径..
            string starupPath = AppDomain.CurrentDomain.BaseDirectory + "QQ农场牧场自动化.exe";
            //class Micosoft.Win32.RegistryKey. 表示Window注册表中项级节点,此类是注册表装.
            //RegistryKey loca = Registry.LocalMachine;
            RegistryKey loca = Registry.CurrentUser;
            RegistryKey run = loca.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

            try
            {
                //SetValue:存储值的名称
                run.SetValue("QQFarmAutomated", starupPath);
                loca.Close();
            }
            catch (Exception ee)
            {
                throw ee;
            }

        }
    }

    public class WebDownload : WebClient
    {
        /// <summary>
        /// Time in milliseconds
        /// </summary>
        public int Timeout { get; set; }

        public WebDownload() : this(5000) { }

        public WebDownload(int timeout)
        {
            this.Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;// https证书
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            var request = base.GetWebRequest(address);
            if (request != null)
            {
                request.Timeout = this.Timeout;
            }
            return request;
        }
    }



    public static partial class Enumerable
    {
        /// <summary>
        /// Split the elements of a sequence into chunks of size at most <paramref name="size"/>.
        /// </summary>
        /// <remarks>
        /// Every chunk except the last will be of size <paramref name="size"/>.
        /// The last chunk will contain the remaining elements and may be of a smaller size.
        /// </remarks>
        /// <param name="source">
        /// An <see cref="IEnumerable{T}"/> whose elements to chunk.
        /// </param>
        /// <param name="size">
        /// Maximum size of each chunk.
        /// </param>
        /// <typeparam name="TSource">
        /// The type of the elements of source.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> that contains the elements the input sequence split into chunks of size <paramref name="size"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="size"/> is below 1.
        /// </exception>
        public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int size)
        {
            if (source == null)
            {
                //ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
                throw new ArgumentNullException("source");
            }

            if (size < 1)
            {
                //ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.size);
                throw new ArgumentNullException("size");
            }

            return ChunkIterator(source, size);
        }

        private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size)
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    TSource[] chunk = new TSource[size];
                    chunk[0] = e.Current;

                    int i = 1;
                    for (; i < chunk.Length && e.MoveNext(); i++)
                    {
                        chunk[i] = e.Current;
                    }

                    if (i == chunk.Length)
                    {
                        yield return chunk;
                    }
                    else
                    {
                        Array.Resize(ref chunk, i);
                        yield return chunk;
                        yield break;
                    }
                }
            }
        }
    }

}
