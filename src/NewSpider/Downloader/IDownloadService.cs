using System.Collections.Generic;
using System.Threading.Tasks;
using NewSpider.Downloader.Entity;

namespace NewSpider.Downloader
{
    public interface IDownloadService
    {
        /// <summary>
        /// 请求分配下载代理器
        /// </summary>
        /// <param name="allotDownloaderMessage"></param>
        /// <returns></returns>
        Task<bool> AllocateAsync(AllotDownloaderMessage allotDownloaderMessage);

        /// <summary>
        /// 把请求发布给下载代理器去下载
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="requests"></param>
        /// <returns></returns>
        Task EnqueueRequests(string ownerId, IEnumerable<Request> requests);
    }
}