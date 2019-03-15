using System.Net.Http;

namespace NewSpider.Downloader
{
    public class Request 
    {
        public string Hash { get; set; }
        public string OwnerId { get; set; }
        public string DownloaderId { get; set; }
        public string Url { get; set; }
        public string Body { get; set; }
        public HttpMethod Method { get; set; }
        
       public int RetriedTimes { get; set; }
    }
}