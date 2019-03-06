using System;

namespace NewSpider.Downloader.Entity
{
    public class DownloaderAgent
    {
        public Guid Id { get; set; }
        
        public string Name { get; set; }
        
        public int ProcessorCount { get; set; }
        
        public int FreeMemory { get; set; }
        
        public int HttpClientCount { get; set; }
        
        public DateTime LastModifcationTime { get; set; }
    }
}