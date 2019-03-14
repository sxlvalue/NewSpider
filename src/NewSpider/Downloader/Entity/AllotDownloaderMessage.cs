namespace NewSpider.Downloader.Entity
{
    public class AllotDownloaderMessage
    {
        public string OwnerId { get; set; }
        public string Domain { get; set; }
        public DownloaderType Type { get; set; }
        public string Cookie { get; set; }
        public string Path { get; set; }
        public bool UseProxy { get; set; }
        public uint Speed { get; set; }
    }
}