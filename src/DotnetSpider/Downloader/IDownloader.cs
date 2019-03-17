using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Downloader
{
    public interface IDownloader
    {
        ILogger Logger { get; set; }
        
        string AgentId { get; set; }
        
        Task<Response> DownloadAsync(Request request);
    }
}