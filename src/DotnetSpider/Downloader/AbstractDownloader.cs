using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Downloader
{
    public abstract class AbstractDownloader : IDownloader
    {
        public ILogger Logger { get; set; }

        public string AgentId { get; set; }

        /// <summary>
        /// 是否下载文件
        /// </summary>
        public bool DownloadFile { get; set; }

        /// <summary>
        /// What mediatype should not be treated as file to download.
        /// </summary>
        /// <summary xml:lang="zh-CN">
        /// 定义哪些类型的内容不需要当成文件下载
        /// </summary>
        public List<string> ExcludeMediaTypes { get; set; } =
            new List<string>
            {
                "",
                "text/html",
                "text/plain",
                "text/richtext",
                "text/xml",
                "text/XML",
                "text/json",
                "text/javascript",
                "application/soap+xml",
                "application/xml",
                "application/json",
                "application/x-javascript",
                "application/javascript",
                "application/x-www-form-urlencoded"
            };     

        protected abstract Task<Response> ImplDownloadAsync(Request request);

        public async Task<Response> DownloadAsync(Request request)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await ImplDownloadAsync(request);
            stopwatch.Stop();
            response.AgentId = AgentId;
            response.Request.AgentId = AgentId;
            response.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return response;
        }
    }
}