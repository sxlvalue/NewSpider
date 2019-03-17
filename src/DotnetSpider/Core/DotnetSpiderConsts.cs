namespace DotnetSpider.Core
{
    public class DotnetSpiderConsts
    {
        public const string ResponseHandlerTopic = "ResponseHandler-";
        public const string DownloaderCenterTopic = "DownloadCenter";
        
        public const string AllocateDownloaderCommand = "Allocate";
        public const string DownloadCommand = "Download";
        public const string RegisterCommand = "Register";
        public const string HeartbeatCommand = "Heartbeat";
        public const string ExitCommand = "Exit";
        public const string CommandSeparator = "|";
    }
}