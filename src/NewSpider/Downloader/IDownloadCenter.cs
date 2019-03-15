using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NewSpider.Downloader.Entity;

namespace NewSpider.Downloader
{
    public interface IDownloadCenter : IDownloadService, IHostedService
    {
    }
}