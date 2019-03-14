using System;

namespace NewSpider.Downloader.Entity
{
    public class DownloaderAgentHeartbeat
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public int ProcessorCount { get; set; }

        public int FreeMemory { get; set; }

        public int DownloaderCount { get; set; }

        public DateTime LastModificationTime { get; set; }
    }
}