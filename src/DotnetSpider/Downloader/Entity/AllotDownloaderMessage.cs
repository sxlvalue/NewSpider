using DotnetSpider.Core;

namespace DotnetSpider.Downloader.Entity
{
    public class AllotDownloaderMessage
    {
        public string OwnerId { get; set; }

        public DownloaderType Type { get; set; }
        
        public Cookie[]  Cookies { get; set; }
        
        public bool UseProxy { get; set; }
        
        public bool UseCookies { get; set; }        
        
        public bool AllowAutoRedirect { get; set; }
        
        public int Timeout { get; set; }
        
        public bool DecodeHtml { get; set; }

        public int DownloaderCount { get; set; }
    }
}