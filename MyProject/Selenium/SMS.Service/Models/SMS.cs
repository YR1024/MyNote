using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSService.Models
{
    public class SMS
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]//数据库是自增才配自增 
        public int ID { get; set; }

        /// <summary>
        /// 短信发件人
        /// </summary>
        [SugarColumn(ColumnDataType = "varchar(100)")]
        public string Name { get; set; }

        /// <summary>
        /// 短信发件人号码
        /// </summary>
        [SugarColumn(ColumnDataType = "varchar(100)")]
        public string Number { get; set; }

        /// <summary>
        /// 短信内容
        /// </summary>
        [SugarColumn(ColumnDataType = "varchar(20000)")]
        public string Content { get; set; }

        /// <summary>
        /// 验证码（如果短信内容里面包含六位数字验证码）
        /// </summary>
        [SugarColumn(ColumnDataType = "varchar(6)")]
        public string Code { get; set; }

        /// <summary>
        /// 短信日期
        /// </summary>
        public DateTime Time { get; set; }

    }
}
