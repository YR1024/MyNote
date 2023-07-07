using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VideoWeb.Helper;
using VideoWeb.Models;

namespace VideoWeb.Controllers
{
    public class HomeController : Controller
    {
        static string path = AppDomain.CurrentDomain.BaseDirectory + "wwwroot\\Video\\";
        static string avpath = path + "AV\\";

        static List<VideoFile> VideoFiles = new List<VideoFile>();

        public IActionResult Index()
        {
            if (!Directory.Exists(avpath))
                Directory.CreateDirectory(avpath);
            //Array FileInfoArray = GetFileHelper.GetFile(avpath, ".mp4.avi.mkv").ToArray();
            //Array.Sort(FileInfoArray, new FileComparer());//按文件创建时间排正序
            var FileInfoArray = GetFileHelper.GetFile(avpath, ".mp4.avi.mkv").ToArray();
            // 按 LastWriteTime 升序排序，然后再按文件名排序
            List<FileInfo> sortedFiles = FileInfoArray.OrderBy(file => file.LastWriteTime)
                                             .ThenBy(file => file.Name)
                                             .ToList();
            ViewBag.ServerPath = avpath;

            VideoFiles.Clear();
            foreach (FileInfo item in sortedFiles)
            {
                var f = new VideoFile();
                f.Name = item.Name;
                f.FullPath = item.FullName;
                f.LastWriteTime = item.LastWriteTime;
                f.Size =(float)Math.Round(item.Length /1024f / 1024f /1024f, 2);
                f.ReleatviePath = item.FullName.Substring(avpath.Length, item.FullName.Length - avpath.Length).Replace("\\","/");
                VideoFiles.Add(f);
            }
            ViewBag.Videos = VideoFiles;

            return View();
        }

        [HttpGet]
        public bool Delete(string FilePath)
        {
            var video = VideoFiles.Where(v => v.ReleatviePath == FilePath).FirstOrDefault();
            if(video != null)
            {
                try
                {
                    System.IO.File.Delete(video.FullPath);
                    return true;
                }
                catch(Exception e)
                {
                    return false;
                }
            }
            return false;
        }

        [HttpGet]
        public bool Merge(List<string> VideoList)
        {
            
            List<VideoFile> VideoFileList = new List<VideoFile>();
            foreach (var video in VideoList)
            {
                var vid = VideoFiles.Where(v => v.ReleatviePath == video).FirstOrDefault();
                if (vid != null) {
                    VideoFileList.Add(vid);
                }
            }
            if (VideoFileList.Count != 2)
                return false; 
           
                return false;

        }

        public IActionResult LOLVideo()
        {
            string LOLpath = path+ "\\League of Legends";
            if (!Directory.Exists(LOLpath))
                Directory.CreateDirectory(LOLpath);
            Array FileInfoArray = GetFileHelper.GetFile(LOLpath, ".mp4.avi.mkv").ToArray();
            Array.Sort(FileInfoArray, new FileComparer());//按文件创建时间排正序
            ViewBag.ServerPath = LOLpath;
            ViewBag.Videos = new List<FileInfo>((IEnumerable<FileInfo>)FileInfoArray);
            return View();
        }


        public IActionResult Index2()
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            Array FileInfoArray = GetFileHelper.GetFile(path, ".mp4.avi.mkv").ToArray();
            Array.Sort(FileInfoArray, new FileComparer());//按文件创建时间排正序
            ViewBag.ServerPath = path;
            ViewBag.Videos = new List<FileInfo>((IEnumerable<FileInfo>)FileInfoArray);
            return View();
        }

        //public Stream GetVideoStream(string filePath)
        //{
        //    string LOLpath = path + "\\League of Legends";
        //    List<FileInfo> FileInfoArray = GetFileHelper.GetFile(LOLpath, ".mp4.avi.mkv");
        //    FileInfoArray[0].
        //    return File.OpenRead(filePath);
        //}


      

        public void VideoClip(string FilePath, string Type, string startTime, string endTime)
        {

            if (Type == "AV")
            {
                //return Redirect("Index");
            }
            else if (Type == "LOL")
            {
                //return Redirect("LOLVideo");
            }

            string Highlights = path + "\\精彩集锦\\";
            if (!Directory.Exists(Highlights))
                Directory.CreateDirectory(Highlights);
            var a = Path.GetFileNameWithoutExtension(FilePath);
            var filename = Highlights + DateTime.Now.ToString("yyyy-MM-dd") + Path.GetFileName(FilePath);


            var startTimeSpan = new TimeSpan();
            var endTimeSpan = new TimeSpan();
            var st = startTime.Split(":");
            var et = endTime.Split(":");
            if (st.Length == 2)
            {
                startTimeSpan = new TimeSpan(0, Convert.ToInt32(st[0]), Convert.ToInt32(st[1]));
                endTimeSpan = new TimeSpan(0, Convert.ToInt32(et[0]), Convert.ToInt32(et[1]));
            }
            else if (st.Length == 3)
            {
                startTimeSpan = new TimeSpan(Convert.ToInt32(st[0]), Convert.ToInt32(st[1]), Convert.ToInt32(st[2]));
                endTimeSpan = new TimeSpan(Convert.ToInt32(et[0]), Convert.ToInt32(et[1]), Convert.ToInt32(et[2]));
            }

            Task.Run(() =>
            {
                try
                {
                    VideoHelper.Cut(FilePath, filename, startTimeSpan, endTimeSpan);
                }
                catch (Exception ex)
                {

                }

            });

        }



        /// <summary>
        /// 删除文件夹以及文件
        /// </summary>
        /// <param name="directoryPath"> 文件夹路径 </param>
        /// <param name="fileName"> 文件名称 </param>
        public static void Delete2(string directoryPath, string fileName)
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

       // void aaa()
       // {
       //     string inputVideoFile = "input_path_goes_here",
       //outputAudioFile = "output_path_goes_here";

       //     new FFMpeg().ExtractAudio(
       //             VideoInfo.FromPath(inputVideoFile),
       //             new FileInfo(outputAudioFile)
       //         );
       // }
    }
}