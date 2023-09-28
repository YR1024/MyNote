using SMSService.Models;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSService.DataBase
{
    public class DB
    {
        //配置数据连接串确保mysql能工作     SMSDB数据库此时并不存在 
        private static readonly string ConnectionStr = "Convert Zero Datetime=True;" +
            "Allow Zero Datetime=True;" +
            "server=z24m.top;" +
            "port=3306;" +
            "Database=SMSDB;" +
            "Uid=root;" +
            "Pwd=sa123;" +
            "SslMode=none;" +
            "min pool size=1";

        public static SqlSugarClient db
        {
            get{

                var cc = new ConnectionConfig()
                {
                    ConnectionString = ConnectionStr,
                    DbType = DbType.MySql,
                    IsAutoCloseConnection = true,
                    InitKeyType = InitKeyType.SystemTable, //已建立数据库和表配置此属性
                    // IsShardSameThread = true

                    //InitKeyType = InitKeyType.Attribute  // 一定要配置此属性 ，否则生成数据库会报错
                };
                return new SqlSugarClient(cc);
            }

        }

        public static void CreateDB()
        {
            db.DbMaintenance.CreateDatabase(); //创建数据库

            db.CodeFirst.InitTables(
                typeof(SMS));

        }

   

    }
}
