using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace QQSpeed_SmartApp.Helper
{
    
    public class MapManager
    {
        public static MapManager Instance = new MapManager();
        private string _configPath = AppDomain.CurrentDomain.BaseDirectory + "//Layout//ModuleSwitch.config";
        public static List<KeyLogInfo> KeyLogList { get; set; } = new List<KeyLogInfo>();

        private MapManager()
        {
            //LoadModuleConfig();
        }

        ~MapManager()
        {
            //SaveModuleConfig();
        }

        //public void SaveModuleConfig()
        //{
        //    using (XmlTextWriter xw = new XmlTextWriter(_configPath, Encoding.Default))
        //    {
        //        xw.Formatting = Formatting.Indented;
        //        xw.IndentChar = '\t';
        //        xw.Indentation = 1;
        //        try
        //        {
        //            XmlSerializer seriesr = new XmlSerializer(KeyLogList.GetType());
        //            seriesr.Serialize(xw, KeyLogList);
        //        }
        //        catch (Exception e)
        //        {
        //            throw e.InnerException;
        //        }
        //    }
        //}

        public static void SaveToMapConfig(List<KeyLogInfo> KeyLogList, string filename)
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory + "Maps\\";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (XmlTextWriter xw = new XmlTextWriter(directory + filename + ".xml", Encoding.Default))
            {
                xw.Formatting = Formatting.Indented;
                xw.IndentChar = '\t';
                xw.Indentation = 1;
                try
                {
                    XmlSerializer seriesr = new XmlSerializer(KeyLogList.GetType());
                    seriesr.Serialize(xw, KeyLogList);
                }
                catch (Exception e)
                {
                    throw e.InnerException;
                }
            }
        }


        public static void ReadMapConfig(string filename)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<KeyLogInfo>));
                var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                KeyLogList = (List<KeyLogInfo>)serializer.Deserialize(fs);
                fs.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //public void LoadModuleConfig()
        //{
        //    try
        //    {
        //        var serializer = new XmlSerializer(typeof(ModuleConfig));
        //        var fs = new FileStream(_configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        //        Parameter = (ModuleConfig)serializer.Deserialize(fs);
        //        fs.Close();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        //Parameter.InitSensorTypeKB();
        //    }
        //}

    }


    [Serializable]
    public class KeyLogInfo
    {
        public Keys Key { get; set; }

        public int DownUp { get; set; }

        public TimeSpan Delay { get; set; }

        public long Ticks
        {
            get { return Delay.Ticks; }
            set
            {
                Delay = new TimeSpan(value);
            }
        }
    }

    public class MapFile
    {
        public string FileName { get; set; }

        public string Path { get; set; }
    }
}
