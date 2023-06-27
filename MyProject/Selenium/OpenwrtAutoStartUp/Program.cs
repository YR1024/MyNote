using HtmlAgilityPack;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenwrtAutoStartUp
{
    class Program
    {
        private static Mutex mutex;

        static void Main(string[] args)
        {
            Task.Run(async () =>
            {

                HttpClient client = new HttpClient();

                HttpContent postContent = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    {"luci_username", "root"},
                    {"luci_password", "password"},
                });
                var temp = await client.PostAsync("http://z24m.top:8024/cgi-bin/luci/", postContent);
                temp = await client.GetAsync("http://z24m.top:8024/cgi-bin/luci/admin/services/wol");
                var result = await temp.Content.ReadAsStringAsync();

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(result);//将字符串转换成 HtmlDocument
                HtmlNode node1 = doc.DocumentNode.SelectSingleNode("//input[@name='token']");
                HtmlNode node2 = doc.DocumentNode.SelectSingleNode("//input[@name='cbi.submit']");
                string token = node1.GetAttributeValue("value", "");
                string cbi = node2.GetAttributeValue("value", "1");

                postContent = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    {"token", token},
                    {"cbi.submit", cbi},
                    {"cbid.wol.1.binary", @"/usr/bin/etherwake"},
                    {"cbid.wol.1.iface", @"br-lan"},
                    {"cbid.wol.1.mac", @"D8:BB:C1:46:4A:DF"},
                });

                temp = await client.PostAsync("http://z24m.top:8024/cgi-bin/luci/admin/services/wol", postContent);
                result = await temp.Content.ReadAsStringAsync();
                Environment.Exit(0);
            });
            Console.ReadLine();
        }

    }

}