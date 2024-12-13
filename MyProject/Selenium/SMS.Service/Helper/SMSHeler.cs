using Newtonsoft.Json;
using SMSService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SMSService.Helper
{
    public class SMSHeler
    {
        public static SMS ParseSMS(string smsjsonStr)
        {
            try
            {
                var smsjson = JsonConvert.DeserializeObject(smsjsonStr, typeof(SMSJson)) as SMSJson;
                var DT = smsjson.Date;
                var time = smsjson.Time.Split('.');
                if (time.Length == 2)
                {
                    var timespan = new TimeSpan(int.Parse(time[0]), int.Parse(time[1]), 0);
                    DT = DT.Add(timespan);
                }

                var sms = new SMS()
                {
                    Name = smsjson.Name,
                    Number = smsjson.Number,
                    Content = smsjson.Content,
                    Code = GetCode(smsjson.Content),
                    Time = DT
                };
                return sms;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
          
        }


        public static string GetCode(string cont)
        {
            string input = "我的字符串包含6位连续的数字123456，还有其他数字，例如78901234567890";
            //string pattern = @"\b\d{6}\b"; // 匹配6位连续的数字  
            string pattern = @"\d{6}"; // 匹配6位连续的数字  

            // 创建正则表达式对象  
            Regex regex = new Regex(pattern);

            // 匹配字符串中的6位数字  
            //Match match = regex.Match(input);
            Match match = Regex.Match(input, pattern);

            // 如果找到匹配项，则输出数字  
            if (match.Success)
            {
                //Console.WriteLine("找到的6位数字是：" + match.Value);
                return match.Value;
            }
            else
            {
                //Console.WriteLine("没有找到6位连续的数字。");
                return string.Empty;
            }
        }
    }
}
