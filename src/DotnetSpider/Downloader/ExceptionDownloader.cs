using System.Threading.Tasks;
using DotnetSpider.Core;

namespace DotnetSpider.Downloader
{
    public class ExceptionDownloader: AbstractDownloader
    {
        protected override Task<Response> ImplDownloadAsync(Request request)
        {
            throw new SpiderException("From exception downloader");
        }
    }
}