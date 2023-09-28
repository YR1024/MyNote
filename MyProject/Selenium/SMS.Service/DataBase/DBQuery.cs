using SMSService.Models;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSService.DataBase
{
    public class DBQuery : IQuery
    {
        public static SqlSugarClient db = DB.db;

        //public static IQuery Instance = new DBQuery();

        public int AddSMS(SMS sms)
        {
            var record = db.Insertable(sms).ExecuteCommand();
            return record;
        }

        public List<SMS> GetAll()
        {
            List<SMS> list = db.Queryable<SMS>().ToList();
            return list;
        }

        public string GetLatestCode(DateTime dt)
        {
            var code = db.Queryable<SMS>()
                        .Where(it => !string.IsNullOrEmpty(it.Code)) //验证码Code字段不为空
                        .OrderBy(it => it.Time, OrderByType.Desc)// 按时间倒序
                        .First(it => it.Time > dt); //时间在传入参数dt后的短信
            return code.Code;
        }

        public string GetLatestCode(DateTime dt, string number)
        {
            var code = db.Queryable<SMS>()
                     .Where(it => !string.IsNullOrEmpty(it.Code)) //验证码Code字段不为空
                     .OrderBy(it => it.Time, OrderByType.Desc)// 按时间倒序
                     .First(it => it.Time > dt && it.Number == number); //时间在传入参数dt后的, 号码为number的短信
            return code.Code;
        }

        public SMS GetLatestSMS()
        {
            var code = db.Queryable<SMS>()
                     .OrderBy(it => it.Time, OrderByType.Desc)// 按时间倒序
                     .First(); //时间在传入参数dt后的短信
            return code;
        }


        public List<SMS> GetSMS(DateTime dt)
        {
            var smsList = db.Queryable<SMS>()
                    .Where(it => it.Time > dt).ToList(); 
            return smsList;
        }

        List<SMS> IQuery.GetLatestSMS(DateTime dt, string number)
        {

            var smsList = db.Queryable<SMS>()
                     .Where(it => it.Time > dt && it.Number == number).ToList();
            return smsList;
        }
    }
}
