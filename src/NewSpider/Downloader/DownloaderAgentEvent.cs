using Newtonsoft.Json.Linq;

namespace NewSpider.Downloader
{
    public class DownloaderAgentEvent
    {
        public string Event { get; set; }
        
        public JObject Content { get; set; }
    }
}