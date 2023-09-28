using SMSService.DataBase;
using SMSService.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SMSService.Server
{

    public class HttpServer
    {
        private static HttpListener listener;

        private static IQuery dbQuery = new DBQuery();

        public void StartUp()
        {
            //启动监听器
            listener.Start();

            Console.WriteLine("Listening...");
            // 处理 POST 请求  
            listener.BeginGetContext(Action, null);
        }

        public HttpServer()
        {
            listener = new HttpListener();
            //定义url及端⼝号，通常设置为配置⽂件
            listener.Prefixes.Add("http://+:5555/");
            Console.WriteLine($"服务端初始化完毕，正在等待客户端请求,时间：{DateTime.Now.ToString()}\r\n");
        }

        static void Action(IAsyncResult ar)
        {
            Console.WriteLine("--Action");

            // 处理 POST 请求  
            listener.BeginGetContext(Action, null);

            var context = listener.EndGetContext(ar);
            var request = context.Request;
            context.Response.AddHeader("Access-Control-Allow-Origin", "*");
            if (request.HttpMethod == "OPTIONS")
            {
                Console.WriteLine("--OPTIONS");
                context.Response.AddHeader("Access-Control-Allow-Headers", "Authorization,Content-Type,Accept,Origin,User-Agent,DNT,Cache-Control,X-Mx-ReqToken,X-Requested-With");
                context.Response.OutputStream.Close();
            }
            if (request.HttpMethod == "POST")
            {
                Console.WriteLine("--POST");
                var response = context.Response;
                var reader = new StreamReader(request.InputStream);
                var content = reader.ReadToEnd();
                Console.WriteLine($"{content}");
                var sms = SMSHeler.ParseSMS(content);
                if(sms != null )
                {
                    dbQuery.AddSMS(sms);
                }
                response.StatusCode = 200;

                //var data = "Success";
                //var jsonModel = JsonConvert.SerializeObject(data);
                // 设置响应头，指定内容类型为 JSON  
                response.ContentType = "application/json";
                // 创建要返回的 JSON 数据  
                string jsonData = "{\"key1\":\"value1\",\"key2\":\"value2\"}";
                // 将 JSON 数据写入响应的 OutputStream 中  
                using (var stream = response.OutputStream)
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.Write(jsonData);
                }
                // 完成响应  
                response.Close();


                //response.OutputStream.Close();
            }
            Console.WriteLine("--Action-End");
        }


    }
}
