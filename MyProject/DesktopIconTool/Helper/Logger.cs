using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopIconTool.Helper
{
    /// <summary>
    /// 日志
    /// </summary>
    public class Logger
    {


        private static string LogPath = AppDomain.CurrentDomain.BaseDirectory + "Log.txt";


        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="strInfo"></param>
        public static void Info(string strInfo)
        {
            //string strPath = ConfigurationManager.AppSettings["FilePath"];
            FileExist(LogPath);
            using (StreamWriter sw = new StreamWriter(LogPath, true))
            {
                sw.WriteLine(DateTime.Now.ToString() + "-------------------" + strInfo);
                sw.Dispose();
                sw.Close();
            }

        }

        private static void FileExist(string fileName)
        {
            //判断文件是否存在
            if (!File.Exists(fileName))
            {
                //创建文件
                try
                {
                    var fileStream = File.Create(fileName);
                    fileStream.Dispose();
                    fileStream.Close();
                }
                catch (Exception e)
                {
                    //Logger.WriteLog(e.Message);
                }
            }
        }

    }
}
