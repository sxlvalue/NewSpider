using System.Threading.Tasks;
using DotnetSpider.Core;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Downloader
{
    public interface IDownloader
    {
        ILogger Logger { get; set; }
        
        string AgentId { get; set; }

        void AddCookies(params Cookie[] cookies);
        
        IHttpProxyPool HttpProxyPool { get; set; }
        
        Task<Response> DownloadAsync(Request request);
    }
}