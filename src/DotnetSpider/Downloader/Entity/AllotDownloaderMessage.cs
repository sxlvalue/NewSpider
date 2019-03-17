namespace DotnetSpider.Downloader.Entity
{
    public class AllotDownloaderMessage
    {
        public string OwnerId { get; set; }
        public string Domain { get; set; }
        public DownloaderType Type { get; set; }
        public string Cookie { get; set; }
        public string Path { get; set; }
        public bool UseProxy { get; set; }
        public double Speed { get; set; }

        public int DownloaderCount { get; set; }
    }
}