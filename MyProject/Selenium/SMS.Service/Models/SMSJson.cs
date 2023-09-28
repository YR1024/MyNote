using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSService.Models
{
    public class SMSJson
    { 
        public string Name { get; set; }
    
        public string Number { get; set; }

        public string Content { get; set; }

        public DateTime Date { get; set; }

        public string Time { get; set; }
    }
}
