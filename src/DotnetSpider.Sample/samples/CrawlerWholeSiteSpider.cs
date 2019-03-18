using System;
using DotnetSpider.Data;
using DotnetSpider.Data.Pipeline;
using DotnetSpider.Data.Processor;
using DotnetSpider.Downloader;

namespace DotnetSpider.Sample.samples
{
    public class CrawlerWholeSiteSpider
    {
        public static void Run()
        {
            var builder = new LocalSpiderBuilder();
            builder.UseSerilog();
            builder.UseDistinctScheduler();
            var spider = builder.Build();
            spider.Id = Guid.NewGuid().ToString("N");
            spider.Name = "CNBLOGS";
            spider.Speed = 5;
            spider.Depth = 3;
            spider.DownloaderType = DownloaderType.Sample;
            spider.AddDataFlow(new DefaultPageProcessor
            {
                Selectable = context => context.GetSelectable(),
                PageFilter = new RegexPageFilter("cnblogs\\.com"),
                TargetRequestResolver = new XpathTargetRequestResolver(".")
            });
            spider.AddRequests("http://www.cnblogs.com/");
            spider.AddProcessor(new DefaultPageProcessor());
            spider.AddPipeline(new ConsolePipeline());

            spider.RunAsync();
        }
    }
}