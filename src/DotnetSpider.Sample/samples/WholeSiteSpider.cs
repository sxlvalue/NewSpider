using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetSpider.Core;
using DotnetSpider.Data;
using DotnetSpider.Data.Parser;
using DotnetSpider.Data.Storage;
using DotnetSpider.Downloader;

namespace DotnetSpider.Sample.samples
{
    public class WholeSiteSpider
    {
        public static void Run1()
        {
            var builder = new LocalSpiderBuilder();
            builder.UseSerilog(); // 可以配置任意日志组件
            builder.UseDistinctScheduler(); // 配置本地内存调度或者数据库调度

            var spider = builder.Build(); // 生成爬虫对象
            spider.Id = Guid.NewGuid().ToString("N"); // 设置任务标识
            spider.Name = "博客园全站采集"; // 设置任务名称
            spider.Speed = 5; // 设置采集速度, 表示每秒下载多少个请求, 大于 1 时越大速度越快, 小于 1 时越小越慢, 不能为0.
            spider.Depth = 3; // 设置采集深度
            spider.DownloaderType = DownloaderType.Default; // 使用普通下载器, 无关 Cookie, 干净的 HttpClient
            spider.AddDataParser(new DataParser
            {
                Selectable = context => context.GetSelectable(ContentType.Html),
                CanParse = DataParser.RegexCanParse("cnblogs\\.com"),
                Follow = DataParser.XpathFollow(".")
            });

            spider.AddStorage(new ConsoleStorage()); // 控制台打印采集结果
            spider.AddRequests("http://www.cnblogs.com/"); // 设置起始链接
            spider.RunAsync(); // 启动
        }

        public static void Run2()
        {
            var builder = new LocalSpiderBuilder();
            builder.UseSerilog(); // 可以配置任意日志组件
            builder.UseDistinctScheduler(); // 配置本地内存调度或者数据库调度

            var spider = builder.Build(); // 生成爬虫对象
            spider.Id = Guid.NewGuid().ToString("N"); // 设置任务标识
            spider.Name = "博客园全站采集"; // 设置任务名称
            spider.Speed = 5; // 设置采集速度, 表示每秒下载多少个请求, 大于 1 时越大速度越快, 小于 1 时越小越慢, 不能为0.
            spider.Depth = 3; // 设置采集深度
            spider.DownloaderType = DownloaderType.Default; // 使用普通下载器, 无关 Cookie, 干净的 HttpClient
            spider.AddDataParser(new CnblogsDataParser());
            spider.AddStorage(new ConsoleStorage()); // 控制台打印采集结果
            spider.AddRequests("http://www.cnblogs.com/"); // 设置起始链接
            spider.RunAsync(); // 启动
        }

        class CnblogsDataParser : DataParser
        {
            public CnblogsDataParser()
            {
                CanParse = RegexCanParse("cnblogs\\.com");
                Follow = XpathFollow(".");
            }

            public override Task<DataFlowResult> Parse(DataFlowContext context)
            {
                var response = context.GetResponse();
                context.AddItem("URL", response.Request.Url);
                context.AddItem("Title", context.GetSelectable().XPath(".//title").GetValue());
                return Task.FromResult(DataFlowResult.Success);
            }
        }
    }
}