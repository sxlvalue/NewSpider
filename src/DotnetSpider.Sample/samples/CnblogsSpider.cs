using System;
using System.Threading.Tasks;
using DotnetSpider.Data;
using DotnetSpider.Data.Parser;
using DotnetSpider.Data.Storage;
using DotnetSpider.Downloader;
using DotnetSpider.Scheduler;

namespace DotnetSpider.Sample.samples
{
    public class CnblogsSpider : Spider
    {
        public CnblogsSpider(IServiceProvider services) : base(services)
        {
        }

        protected override void Initialize()
        {
            Scheduler = new QueueDistinctBfsScheduler();
            Speed = 1;
            Depth = 3;
            DownloaderType = DownloaderType.Default;
            AddDataFlow(new CnblogsDataParser()).AddDataFlow(new JsonFileStorage());
            AddRequests("http://www.cnblogs.com/");
        }

        class CnblogsDataParser : DataParser
        {
            public CnblogsDataParser()
            {
                CanParse = RegexCanParse("cnblogs\\.com");
                Follow = XpathFollow(".");
            }

            protected override Task<DataFlowResult> Parse(DataFlowContext context)
            {
                var response = context.GetResponse();
                context.AddItem("URL", response.Request.Url);
                context.AddItem("Title", context.GetSelectable().XPath(".//title").GetValue());
                return Task.FromResult(DataFlowResult.Success);
            }
        }
    }
}