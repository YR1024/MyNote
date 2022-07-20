using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VideoWeb.Helper;
using VideoWeb.Models;

namespace VideoWeb.Controllers
{
    public class HomeController : Controller
    {
        string path = AppDomain.CurrentDomain.BaseDirectory + "Video";

        public IActionResult Index()
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            Array FileInfoArray = GetFileHelper.GetFile(path, ".mp4.avi.mkv").ToArray();
            Array.Sort(FileInfoArray, new FileComparer());//按文件创建时间排正序
            ViewBag.ServerPath = path;
            ViewBag.Videos = new List<FileInfo>((IEnumerable<FileInfo>)FileInfoArray);
            return View();
        }

        public ActionResult Delete(string FilePath)
        {
            System.IO.File.Delete(path + FilePath);
            return Redirect("Index");
        }
        /// <summary>
        /// 删除文件夹以及文件
        /// </summary>
        /// <param name="directoryPath"> 文件夹路径 </param>
        /// <param name="fileName"> 文件名称 </param>
        public static void Delete(string directoryPath, string fileName)
        {

            //删除文件
            for (int i = 0; i < Directory.GetFiles(directoryPath).ToList().Count; i++)
            {
                if (Directory.GetFiles(directoryPath)[i] == fileName)
                {
                    System.IO.File.Delete(fileName);
                }
            }

            //删除文件夹
            for (int i = 0; i < Directory.GetDirectories(directoryPath).ToList().Count; i++)
            {
                if (Directory.GetDirectories(directoryPath)[i] == fileName)
                {
                    Directory.Delete(fileName, true);
                }
            }
        }

        /// <summary>
        /// 获得指定路径下所有文件名
        /// </summary>
        /// <param name="path">文件路径</param>
        public static List<string> getFileName(string path)
        {
            List<string> fileNames = new List<string>();
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] files = di.GetFiles(); //获得该文件夹下的文件，返回类型为FileInfo
            foreach (var fi in files)
            {
                fileNames.Add("Video/" + fi.Name);
            }
            return fileNames;
        }
    }
}