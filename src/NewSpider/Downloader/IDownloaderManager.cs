using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NewSpider.Downloader.Entity;

namespace NewSpider.Downloader
{
    public interface IDownloaderManager
    {
        Task RegisterAsync(DownloaderOptions options);

        Task PublishAsync(IEnumerable<IRequest> requests);
        
        IDownloaderAgentStore Store { get; }
    }
}