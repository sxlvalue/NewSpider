using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace NewSpider.Downloader
{
    public interface IDownloaderAgent
    {
        HttpClient GetHttpClientAsync(string ownerId);

        Task InitHttpClientAsync(string ownerId, string domain, string cookie, bool useProxy);

        Task CleanAsync();

        Task StartHeartbeatAsync();

        Task<List<object>> DownloadAsync(string ownerId, IEnumerable<IRequest> toObject);
    }
}