using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace R_Auto_Task
{
    class TaskManager
    {
        public static TaskManager Instance = new TaskManager();

        private string _configPath = AppDomain.CurrentDomain.BaseDirectory + "Tasks.config";

        public ObservableCollection<Operation> OperationList { get; set; }

        protected TaskManager()
        {
        }

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public void LoadConfig()
        {
            if (!File.Exists(_configPath))
                return;

            try
            {
                var serializer = new XmlSerializer(typeof(ObservableCollection<Operation>));
                var fs = new FileStream(_configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                OperationList = (ObservableCollection<Operation>)serializer.Deserialize(fs);
                fs.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        public void SaveConfig()
        {
            using (XmlTextWriter xw = new XmlTextWriter(_configPath, Encoding.Default))
            {
                xw.Formatting = Formatting.Indented;
                xw.IndentChar = '\t';
                xw.Indentation = 1;
                try
                {
                    XmlSerializer seriesr = new XmlSerializer(OperationList.GetType());
                    seriesr.Serialize(xw, OperationList);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
