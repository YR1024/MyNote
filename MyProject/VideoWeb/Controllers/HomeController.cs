using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using VideoWeb.Data;
using VideoWeb.Helper;
using VideoWeb.Models;

namespace VideoWeb.Controllers
{
    public class HomeController : Controller
    {

        private readonly VideoDbContext _db; // 数据库上下文

        static string path = AppDomain.CurrentDomain.BaseDirectory + "wwwroot\\Video\\";
        static string avpath = path + "AV\\";
        static string starVideoConfig = AppDomain.CurrentDomain.BaseDirectory + "StarVideoConfig.json";


        // 构造函数注入数据库
        public HomeController(VideoDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            if (!Directory.Exists(avpath))
                Directory.CreateDirectory(avpath);

            // 1. 扫描物理硬盘文件
            var fileInfoArray = GetFileHelper.GetFile(avpath, ".mp4.avi.mkv.wmv").ToArray();
            List<FileInfo> sortedFiles = fileInfoArray.OrderBy(file => file.LastWriteTime)
                                                      .ThenBy(file => file.Name)
                                                      .ToList();

            // 2. 将硬盘上新发现的视频同步到数据库中
            SyncFilesToDatabase(sortedFiles);

            // 3. 从数据库中查询所有视频（使用 Include 提前加载演员和标签，防止空引用报错）
            var videos = _db.Videos
                            .Include(v => v.Actors)
                            .Include(v => v.Tags)
                            .OrderBy(v => v.LastWriteTime)
                            .ToList();

            ViewBag.ServerPath = avpath;
            ViewBag.Videos = videos; // 现在传给前端的是从数据库查出来的 List<Video>

            return View();
        }

        // 同步方法：比对物理文件和数据库
        private void SyncFilesToDatabase(List<FileInfo> files)
        {
            bool hasChanges = false;

            // 获取之前旧的 JSON 收藏记录（方便你平滑过渡，以后可以删掉）
            var oldStarVideos = GetStarVideo();

            foreach (var item in files)
            {
                var relPath = item.FullName.Substring(avpath.Length).Replace("\\", "/");

                // 如果数据库里不存在这个相对路径的视频，就把它加进去
                if (!_db.Videos.Any(v => v.RelativePath == relPath))
                {
                    var newVideo = new Video
                    {
                        Name = item.Name,
                        RelativePath = relPath,
                        FullPath = item.FullName,
                        Size = (float)Math.Round(item.Length / 1024f / 1024f / 1024f, 2),
                        LastWriteTime = item.LastWriteTime,
                        // 判断是否在旧的 JSON 收藏里
                        IsStar = oldStarVideos.Exists(s => s == item.FullName)
                    };
                    _db.Videos.Add(newVideo);
                    hasChanges = true;
                }
            }

            // 如果有新视频插入，统一保存到数据库
            if (hasChanges)
            {
                _db.SaveChanges();
            }
        }

     
        public IActionResult Index24()
        {
            if (!Directory.Exists(avpath))
                Directory.CreateDirectory(avpath);

            var videos = _db.Videos
                            .Include(v => v.Actors)
                            .Include(v => v.Tags)
                            .OrderBy(v => v.LastWriteTime)
                            .ToList();

            ViewBag.ServerPath = avpath;
            ViewBag.Videos = videos; // 现在传给前端的是从数据库查出来的 List<Video>

            return View();
        }

        List<string> GetStarVideo()
        {
            List<string> starVideo;
            starVideo = JsonSerializeHelper.ReadJsonFile<List<string>>(starVideoConfig);
            if (starVideo == null) { 
                starVideo = new List<string>();
            }
            return starVideo;
        }



        [HttpGet]
        public bool Star(string FilePath, bool IsStar)
        {
            List<string> starVideo = GetStarVideo()?? new List<string>();
            if (IsStar)
            {
                if (!starVideo.Contains(FilePath))
                {
                    starVideo.Add(FilePath);
                }
            }
            else
            {
                if (starVideo.Contains(FilePath))
                {
                    starVideo.Remove(FilePath);
                }
            }
            JsonSerializeHelper.SaveObjectToJsonFile(starVideo, starVideoConfig);
            return true;
        }

        [HttpGet]
        public bool Delete(string FilePath)
        {
            var video = _db.Videos.Where(v => v.FullPath == FilePath).FirstOrDefault();
            if(video != null)
            {
                try
                {
                    System.IO.File.Delete(video.FullPath);
                    _db.Videos.Remove(video);
                    _db.SaveChanges();
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
        public bool Rename(string FilePath,string NewName)
        {
            var video = _db.Videos.Where(v => v.FullPath == FilePath).FirstOrDefault();
            if (video != null)
            {
                try
                {
                    string extensionname = Path.GetExtension(video.FullPath);
                    string newPath = Path.GetDirectoryName(video.FullPath) + "\\" + NewName + extensionname;
                    System.IO.File.Move(video.FullPath, newPath);

                    FileInfo fileInfo = new FileInfo(newPath);
                    video.Name = fileInfo.Name;
                    video.RelativePath = newPath.Substring(avpath.Length).Replace("\\", "/");
                    video.FullPath = fileInfo.FullName;
                    _db.SaveChanges();
                    return true;
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            return false;
        }

        [HttpGet]
        public async Task<string> Merge(string file1, string file2)
        {
            var f1 = _db.Videos.Where(v => v.RelativePath == file1).FirstOrDefault();
            var f2 = _db.Videos.Where(v => v.RelativePath == file2).FirstOrDefault();
            if (f1 != null && f2 != null)
            {
                if (System.IO.File.Exists(f1.FullPath) && System.IO.File.Exists(f2.FullPath))
                {
                    var newVideoFileName = VideoHelper.FindLongestCommonSubstring(f1.Name, f2.Name);
                    newVideoFileName = $"{avpath}{newVideoFileName}.mp4";
                    bool r = await VideoHelper.Combine(f1.FullPath, f2.FullPath, newVideoFileName);
                    //bool r = await VideoHelper.MergeVideo(f1.FullPath, f2.FullPath, newVideoFileName);
                    if (r)
                    {
                        System.IO.File.Delete(f1.FullPath);
                        System.IO.File.Delete(f2.FullPath);
                        return "视频合并完成！";
                    }
                    else
                    {
                        return "视频合并失败！";
                    }
                }
                else
                {
                    return "文件不存在！";
                }
            }
            else
            {
                return "集合中的项不存在，请刷新网页";
            }

            //return $"{file1},{file2}";
        }

        [HttpGet]
        public async Task<string> GetAllFanHao()
        {
            var videos = _db.Videos.ToList();
            int count = 0; int errorCount = 0;
            foreach(var video in videos)
            {
                string fanhao = GetFanHao(video.Name);
                if (!string.IsNullOrEmpty(fanhao))
                {
                    //Console.WriteLine("提取到的番号：" + fanhao);
                    if(video.Code != fanhao)
                    {
                        video.Code = fanhao;
                        count++;
                    }
                    
                }
                else
                {
                    //Console.WriteLine("未找到番号");
                    errorCount++;
                }
            }
            _db.SaveChanges();

            return $"已处理 {count} 个视频，{errorCount}个视频未找到番号 ";
            //return $"{file1},{file2}";
        }
        

        public IActionResult LOLVideo()
        {
            string LOLpath = path+ "\\League of Legends";
            if (!Directory.Exists(LOLpath))
                Directory.CreateDirectory(LOLpath);
            Array FileInfoArray = GetFileHelper.GetFile(LOLpath, ".mp4.avi.mkv.wmv").ToArray();
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

        /// <summary>
        /// 从字符串中提取番号，无则返回空
        /// </summary>
        static string GetFanHao(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // 番号正则：2-6位字母 + - + 2-5位数字
            string pattern = @"[A-Za-z]{2,6}-\d{2,5}";

            // 匹配
            Match match = Regex.Match(input, pattern);

            // 匹配成功返回番号，失败返回空
            return match.Success ? match.Value : string.Empty;
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