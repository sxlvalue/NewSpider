 
namespace NewSpider.Downloader
{
    public class ResponseEvent  
    {
        public IRequest Request { get; }
        public string Content { get; set; }
    }
}