using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMSService.DataBase;
using SMSService.Models;
using SMSService.Server;

namespace SMSService
{
    internal class Program
    {
        static IQuery dbQuery = new DBQuery();

        static void Main(string[] args)
        {


            HttpServer httpServer = new HttpServer();
            httpServer.StartUp();

            //var a = dbQuery.GetAll();
            ////var b = dbQuery.AddCount(SMS sms);
            //var c = dbQuery.GetLatestSMS();
            //var d = dbQuery.GetSMS(DateTime.Now);
            //var d2 = dbQuery.GetSMS(new DateTime(2023, 09, 01));
            //var e1 = dbQuery.GetLatestSMS(new DateTime(2023, 09, 01), "17347980440");
            //var e2 = dbQuery.GetLatestSMS(new DateTime(2023, 09, 01), "17347980442");
            //var f = dbQuery.GetLatestCode(new DateTime(2023, 09, 01));
            //var g = dbQuery.GetLatestCode(new DateTime(2023, 09, 01), "17347980440");
            //Console.WriteLine($"{a}\n{c}\n{d}\n{d2}\n{e1}\n{e2}\n{f}\n{g}");
            Console.ReadLine(); 
        }
    }
}
