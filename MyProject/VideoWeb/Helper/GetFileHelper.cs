using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace VideoWeb.Helper
{
    public static class GetFileHelper
    {
        /// <summary>
        /// 私有变量
        /// </summary>
        private static List<FileInfo> lst = new List<FileInfo>();

        /// <summary>
        /// 获得目录下所有文件或指定文件类型文件(包含所有子文件夹)
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <param name="extName">扩展名可以多个 例如 .mp3.wma.rm</param>
        /// <returns>List<FileInfo></returns>
        public static List<FileInfo> getFile(string path, string extName)
        {
            getdir(path, extName);
            return lst;
        }

        /// <summary>
        /// 私有方法,递归获取指定类型文件,包含子文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <param name="extName"></param>
        private static void getdir(string path, string extName)
        {
            try
            {
                string[] dir = Directory.GetDirectories(path); //文件夹列表   
                DirectoryInfo fdir = new DirectoryInfo(path);
                FileInfo[] file = fdir.GetFiles();
                //FileInfo[] file = Directory.GetFiles(path); //文件列表   
                if (file.Length != 0 || dir.Length != 0) //当前目录文件或文件夹不为空                   
                {
                    foreach (FileInfo f in file) //显示当前目录所有文件   
                    {
                        if (extName.ToLower().IndexOf(f.Extension.ToLower()) >= 0)
                        {
                            lst.Add(f);
                        }
                    }
                    foreach (string d in dir)
                    {
                        getdir(d, extName);//递归   
                    }
                }
            }
            catch (Exception ex)
            {
                //LogHelper.WriteLog(ex);
                throw ex;
            }
        }

        /// <summary>
        /// 获得目录下所有文件或指定文件类型文件(包含所有子文件夹)
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <param name="extName">扩展名可以多个 例如 .mp3.wma.rm</param>
        /// <returns>List<FileInfo></returns>
        public static List<FileInfo> GetFile(string path, string extName)
        {
            try
            {
                List<FileInfo> lst = new List<FileInfo>();
                string[] dir = Directory.GetDirectories(path); //文件夹列表   
                DirectoryInfo fdir = new DirectoryInfo(path);
                FileInfo[] file = fdir.GetFiles();
                //FileInfo[] file = Directory.GetFiles(path); //文件列表   
                if (file.Length != 0 || dir.Length != 0) //当前目录文件或文件夹不为空                   
                {
                    foreach (FileInfo f in file) //显示当前目录所有文件   
                    {
                        if (extName.ToLower().IndexOf(f.Extension.ToLower()) >= 0 && f.Extension　!= String.Empty)
                        {
                            lst.Add(f);
                        }
                    }
                    foreach (string d in dir)
                    {
                        lst.AddRange( GetFile(d, extName) );//递归   
                    }
                }
                return lst;
            }
            catch (Exception ex)
            {
                //LogHelper.WriteLog(ex);
                throw ex;
            }
        }


    }

    public class JsonSerializeHelper
    {
        public static string SerializeObject(object obj)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            return json;
        }

        public static T DeserializeObject<T>(string json)
        {
            T obj = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            return obj;
        }


        // 新增方法：从指定路径读取JSON文件并反序列化为对象
        public static T ReadJsonFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                //创建文件
                File.Create(filePath).Close();
                return default;
            }

            string json = File.ReadAllText(filePath);
            T obj = JsonConvert.DeserializeObject<T>(json);
            return obj;
        }

        // 新增方法：将对象序列化为JSON格式并保存到指定路径的文件中
        public static void SaveObjectToJsonFile(object obj, string filePath)
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }   
}