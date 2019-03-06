using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace NewSpider.Downloader
{
    public interface IDownloaderAgent : IHostedService
    {
    }
}