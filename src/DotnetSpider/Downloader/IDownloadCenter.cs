using Microsoft.Extensions.Hosting;

namespace DotnetSpider.Downloader
{
    public interface IDownloadCenter : IDownloadService, IHostedService
    {
    }
}