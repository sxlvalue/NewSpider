using Newtonsoft.Json;

namespace NewSpider.Infrastructure
{
    public class NewSpiderConsts
    {
        public const string ResponseHandlerTopic = "ResponseHandler-";
        public const string LocalDownloaderAgentTopic = "LocalDownloaderAgent";
        public const string DownloaderCenterTopic = "DownloadCenter";
        
        public const string AllocateDownloaderCommand = "Allocate";
        public const string DownloadCommand = "Download";
        public const string RegisterCommand = "Register";
        public const string HeartbeatCommand = "Heartbeat";
        public const string CommandSeparator = "|";

    }
}