using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.IO;
using System.Text.Json;
using VideoWeb.Server.Helper;
using VideoWeb.Server.Models;

namespace VideoWeb.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {

        //[HttpGet(Name = "base")]
        //public string GetBasePath()
        //{
        //    return AppDomain.CurrentDomain.BaseDirectory;
        //}
        [HttpGet("base")]
        public string GetBasePath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        // 接口 2: GET api/Values/getAllVideoFiles?path={path}
        /// <summary>
        /// 获取所有视频文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpGet("getAllVideoFiles")]
        public string GetFiles(string path)
        {

            if (!Directory.Exists(path))
                return "路径不存在";
            var FileInfoArray = GetFileHelper.GetFile(path, ".mp4.avi.mkv.wmv").ToArray();
            // 按 LastWriteTime 升序排序，然后再按文件名排序
            List<FileInfo> sortedFiles = FileInfoArray.OrderBy(file => file.LastWriteTime)
                                             .ThenBy(file => file.Name)
                                             .ToList();

            var VideoFiles = new List<VideoFile>();
            foreach (FileInfo item in sortedFiles)
            {
                var f = new VideoFile();
                f.Name = item.Name;
                f.FullPath = item.FullName;
                f.LastWriteTime = item.LastWriteTime;
                f.Size = (float)Math.Round(item.Length / 1024f / 1024f / 1024f, 2);
                f.ReleatviePath = item.FullName.Substring(path.Length, item.FullName.Length - path.Length).Replace("\\", "/");
                VideoFiles.Add(f);
            }
            return JsonSerializer.Serialize(VideoFiles);
        }



        [HttpGet("video")]
        public IActionResult GetVideoStream(string _videoPath)
        {
            if (!System.IO.File.Exists(_videoPath))
                return NotFound();

            //var stream = new FileStream(_videoPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536, useAsync: true);
            //return File(stream, GetContentType(_videoPath), enableRangeProcessing: true);

            if (!System.IO.File.Exists(_videoPath))
            {
                return NotFound();
            }
            // 使用 PhysicalFile 代替 FileStream（自动处理资源释放）
            return PhysicalFile(_videoPath, "video/mp4", enableRangeProcessing: true);
        }
     
        private string GetContentType(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            switch (extension)
            {
                case ".mp4":
                    return "video/mp4";
                case ".webm":
                    return "video/webm";
                case ".ogg":
                    return "video/ogg";
                case ".avi":
                    return "video/x-msvideo";
                case ".flv":
                    return "video/x-flv";
                case ".mov":
                    return "video/quicktime";
                case ".mkv":
                    return "video/x-matroska";
                default:
                    return "application/octet-stream"; // 默认类型
            }
        }


        // 接口 3: GET api/Values/user/{id}
        [HttpGet("user/{id}")]
        public IActionResult GetUser(int id)
        {

            //        // 插入视频并关联标签
            //        var video = new Video
            //        {
            //            Title = "My Video",
            //            Tags = new List<Tag>
            //{
            //    new Tag { Name = "科技" },
            //    new Tag { Name = "教程" }
            //}
            //        };
            //        db.InsertNav(video).Include(v => v.Tags).ExecuteCommand();


            //        var videos = db.Queryable<Video>()
            //.Includes(v => v.Tags) // 自动加载关联的 Tags
            //.ToList();



            //// 查询视频及其关联的标签
            //var videos = db.Queryable<Video>()
            //    .Includes(v => v.Tags)
            //    .ToList();

            //// 查询标签及其关联的视频
            //var tags = db.Queryable<Tag>()
            //    .Includes(t => t.Videos)
            //    .ToList();


            //// 查询视频及其标签
            //var video = db.Queryable<VideoFile>()
            //    .Includes(v => v.Tags) // 自动加载关联的标签
            //    .First();

            //// 输出标签
            //foreach (var tag in video.Tags)
            //{
            //    Console.WriteLine(tag.Name); // 输出：巨乳、少女等
            //}
            return Ok($"User {id}");
        }

        // 接口 4: POST api/Values/upload
        [HttpPost("upload")]
        public IActionResult UploadFile(IFormFile file)
        {
            return Ok($"Received {file.FileName}");
        }

        // 接口 5: GET api/Values/search?keyword={keyword}
        [HttpGet("search")]
        public IActionResult Search(string keyword)
        {
            return Ok($"Searching for {keyword}");
        }
    }
}
