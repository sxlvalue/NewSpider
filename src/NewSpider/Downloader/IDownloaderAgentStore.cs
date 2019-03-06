using System.Collections.Generic;
using System.Threading.Tasks;
using NewSpider.Downloader.Entity;

namespace NewSpider.Downloader
{
    public interface IDownloaderAgentStore
    {
        Task<IEnumerable<DownloaderAgent>> GetAvailableAsync();

        Task RegisterAsync(DownloaderAgent agent);

        Task HeartbeatAsync(DownloaderAgent agent);
    }
}