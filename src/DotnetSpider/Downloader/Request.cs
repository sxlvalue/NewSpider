using System.Net.Http;

namespace DotnetSpider.Downloader
{
    public class Request
    {
        public string Hash { get; set; }

        public string OwnerId { get; set; }        

        public string AgentId { get; set; }

        /// <summary>
        /// 链接的深度，用户不得修改
        /// </summary>
        public int Depth { get; set; }

        public string Url { get; set; }

        public string Body { get; set; }

        public HttpMethod Method { get; set; }

        public int RetriedTimes { get; set; }
    }
}