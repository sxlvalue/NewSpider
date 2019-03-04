using System.Net.Http;

namespace NewSpider
{
    public interface IRequest
    {
        string Hash { get; set; }

        string OwnerId { get; set; }
        
        string DownloaderId { get; set; }
        
        string Url { get; set; }
        
        string Body { get; set; }
        
        HttpMethod Method { get; set; }
    }
}