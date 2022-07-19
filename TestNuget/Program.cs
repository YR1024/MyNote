using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace TestNuget
{
    class Program
    {

        [STAThread]
        static void Main(string[] args)
        {
            //QQSignIn.Start();


            Task.Run(() =>
            {
                Quickspot.LoadImage1();
                Quickspot.LoadImage2();
            });

            while (true)
            {
                if (Quickspot.IsImage1Loaded && Quickspot.IsImage2Loaded)
                {
                    break;
                }
            }

            Task.Run(() =>
            {
                Quickspot.Compare();
            });

            while (true)
            {
                if (Quickspot.Result.Count == Quickspot.sourceImg.Count)
                {
                    break;
                }
            }

            Thread t = new Thread(() =>
            {
                CompareResult compareResult = new CompareResult(Quickspot.Result);
                compareResult.ShowCompareResult();
                Dispatcher.Run();
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();


            //OCR ocr = new OCR();
            //ocr.Test();
            Console.ReadLine();




        }




    }

  
}
