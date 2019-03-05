using System.Collections.Generic;
using NewSpider.Infrastructure;

namespace NewSpider.Downloader
{
    public class LocalDownloaderAgentStore : IDownloaderAgentStore
    {
        public IEnumerable<object> GetAllLists()
        {
            return LocalStore.Downloaders.Keys;
        }
    }
}