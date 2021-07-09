using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notes
{
    public class Helper
    {

        /// <summary>
        /// C#计算函数执行的时间
        /// </summary>
        static void  CalcCodeImplementCostTime()
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start(); //  开始监视代码

            /*要执行的
             *函数或
             *代码片段
             */
            stopwatch.Stop(); //  停止监视
            TimeSpan timeSpan = stopwatch.Elapsed; //  获取总时间
            double hours = timeSpan.TotalHours; // 小时
            double minutes = timeSpan.TotalMinutes;  // 分钟
            double seconds = timeSpan.TotalSeconds;  //  秒数
            double milliseconds = timeSpan.TotalMilliseconds;  //  毫秒数
        }

    }
}
