using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace VideoWeb.Controllers
{
    public class VideosController : Controller
    {
        //public HttpResponseMessage Get(string filename, string ext)
        //{
        //    var video = new VideoStream(filename, ext);
        //    Action<Stream, HttpContent, TransportContext> send = video.WriteToStream;
        //    var response = HttpRequestMessageExtensions.CreateResponse(new HttpRequestMessage()) ;
        //    response.Content = new PushStreamContent(send, new MediaTypeHeaderValue("video/" + ext));
        //    //调用异步数据推送接口
        //    return response;
        //}


        public IActionResult Index()
        {
            return View();
        }

        public HttpResponseMessage GetVideoStream(string filename, string ext)
        {
            var video = new VideoStream(filename, ext);
            Action<Stream, HttpContent, TransportContext> send = video.WriteToStream;
            var response = HttpRequestMessageExtensions.CreateResponse(new HttpRequestMessage());
            response.Content = new PushStreamContent(send, new MediaTypeHeaderValue("video/" + ext));
            //调用异步数据推送接口
            return response;
        }
    }
}
