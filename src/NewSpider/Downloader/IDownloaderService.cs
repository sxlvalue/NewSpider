using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace NewSpider.Downloader
{
    public interface IDownloaderService : IHostedService
    {
        Task RegisterAsync(string ownerId, int nodeCount, int threadNum, string domain = null, string cookie = null,
            bool useProxy = false, bool inherit = false);

        Task PublishAsync(string ownerId, IEnumerable<IRequest> requests);

        Task ShutDownDownloader(string downloaderId);

        Task ExcludeDownloader(string ownerId, string downloaderId);
    }
}