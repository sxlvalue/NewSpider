using System.Threading.Tasks;

namespace NewSpider.Downloader
{
    public interface IDownloader
    {
        Task<Response> DownloadAsync(Request request);
    }
}