using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrpClientManager
{
    /// <summary>
    /// 日志
    /// </summary>
    public class Logger
    {
        private static string LogPath = AppDomain.CurrentDomain.BaseDirectory + "Log.txt";

        public static Logger Instance { get; private set; } = new Logger();

        private Logger()
        {
            FileExist(LogPath);
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="strInfo"></param>
        public void WriteLog(string strInfo)
        {
            try
            {
                if (strInfo == null) return;
                using (StreamWriter sw = new StreamWriter(LogPath, true))
                {
                    sw.WriteLine(DateTime.Now.ToString() + "-------------------" + strInfo);
                    sw.Dispose();
                    sw.Close();
                }
            }
            catch(Exception e)
            {
                System.IO.File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "ErrorLog.txt", e.Message + e.StackTrace);
            }
         
        }

        private void FileExist(string fileName)
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


        private void DirExist(string pathName)
        {
            //判断文件是否存在
            if (!Directory.Exists(pathName))
            {
                //创建文件
                try
                {
                    Directory.CreateDirectory(pathName);
                }
                catch (Exception e)
                {
                    WriteLog("创建目录失败：" + e.Message);
                }
            }
        }

    }
}
