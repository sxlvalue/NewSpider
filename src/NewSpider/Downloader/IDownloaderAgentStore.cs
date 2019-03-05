using System.Collections.Generic;

namespace NewSpider.Downloader
{
    public interface IDownloaderAgentStore
    {
        IEnumerable<object> GetAllLists();
    }
}