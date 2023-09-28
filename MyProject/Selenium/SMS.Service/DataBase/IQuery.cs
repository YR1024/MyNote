using SMSService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSService.DataBase
{
    public interface IQuery
    {
        /// <summary>
        /// 获取所有短信
        /// </summary>
        /// <returns></returns>
        List<SMS> GetAll();

        /// <summary>
        /// 增加一条SMS记录
        /// </summary>
        /// <param name="sms"></param>
        /// <returns></returns>
        int AddSMS(SMS sms);

        /// <summary>
        /// 获取最新的短信
        /// </summary>
        /// <returns></returns>
        SMS GetLatestSMS();

        /// <summary>
        /// 获取短信
        /// </summary>
        /// <param name="dt">在dt时间后的短信</param>
        /// <returns></returns>
        List<SMS> GetSMS(DateTime dt);

        /// <summary>
        /// 获取短信
        /// </summary>
        /// <param name="dt">在dt时间后的短信</param>
        /// <param name="number">指定发件人的号码</param>
        /// <returns></returns>
        List<SMS> GetLatestSMS(DateTime dt, string number);

        /// <summary>
        /// 获取最新的验证码
        /// </summary>
        /// <param name="dt">在dt时间后的验证码</param>
        /// <returns></returns>
        string GetLatestCode(DateTime dt);

        /// <summary>
        /// 获取最新的验证码
        /// </summary>
        /// <param name="dt">在dt时间后的验证码</param>
        /// <param name="number">指定发件人的号码</param>
        /// <returns></returns>
        string GetLatestCode(DateTime dt, string number);

        
    }
}
