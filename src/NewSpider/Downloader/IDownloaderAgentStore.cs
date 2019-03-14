using System.Collections.Generic;
using System.Threading.Tasks;
using NewSpider.Downloader.Entity;

namespace NewSpider.Downloader
{
    public interface IDownloaderAgentStore
    {
        Task<IEnumerable<DownloaderAgentHeartbeat>> GetAvailableAsync();

        Task RegisterAsync(DownloaderAgentHeartbeat agent);

        Task HeartbeatAsync(DownloaderAgentHeartbeat agent);
        
        Task AllocateAsync(string ownerId, IEnumerable<string> agnetIds);
    }
}