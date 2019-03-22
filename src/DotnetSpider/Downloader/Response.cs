namespace DotnetSpider.Downloader
{
    public class Response
    {
        public Request Request { get; set; }

        public string Exception { get; set; }

        public string RawText { get; set; }

        public string AgentId { get; set; }

        public bool Success { get; set; }

        public long ElapsedMilliseconds { get; set; }
    }
}