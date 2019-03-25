using System.Threading.Tasks;
using DotnetSpider.Downloader.Entity;

namespace DotnetSpider.Downloader
{
    public interface IDownloaderAllocator
    {
        Task<DownloaderEntry> CreateDownloaderEntryAsync(
            AllotDownloaderMessage allotDownloaderMessage);
    }
}