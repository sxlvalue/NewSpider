namespace NewSpider.Downloader.Entity
{
    public class DownloaderOptions
    {
        public string OwnerId { get; set; }
        public uint DownloaderCount { get; set; }
        public string Domain { get; set; }
        public string Cookie { get; set; }
        public string Path { get; set; }
        public bool UseProxy { get; set; }
    }
}