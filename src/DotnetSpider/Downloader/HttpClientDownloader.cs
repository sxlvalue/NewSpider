using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DotnetSpider.Downloader
{
    public class HttpClientDownloader : AbstractDownloader
    {

        
        /// <summary>
        /// 是否自动跳转
        /// </summary>
        public bool AllowAutoRedirect { get; set; } = true;  
        
        protected HttpClient HttpClient { get; set; } = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            UseProxy = true,
            UseCookies = true,
            MaxAutomaticRedirections = 10
        });
        
        protected override Task<Response> ImplDownloadAsync(Request request)
        {
            return null;
        }
    }
}