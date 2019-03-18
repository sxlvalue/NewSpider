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
            builder.UseSerilog(); // 可以配置任意日志组件
            builder.UseDistinctScheduler(); // 配置本地内存调度或者数据库调度

            var spider = builder.Build(); // 生成爬虫对象
            spider.Id = Guid.NewGuid().ToString("N"); // 设置任务标识
            spider.Name = "博客园全站采集"; // 设置任务名称
            spider.Speed = 5; // 设置采集速度, 表示每秒下载多少个请求, 大于 1 时越大速度越快, 小于 1 时越小越慢, 不能为0.
            spider.Depth = 3; // 设置采集深度
            spider.DownloaderType = DownloaderType.Sample; // 使用普通下载器, 无关 Cookie, 干净的 HttpClient
            spider.AddProcessor(new DefaultPageProcessor
            {
                Selectable = context => context.GetSelectable(), // 设置根选择器的构造，可以是 Html 或者 Json
                PageFilter = new RegexPageFilter("cnblogs\\.com"), // 是否执行解析的判断
                TargetRequestResolver = new XpathTargetRequestResolver(".") // 目标链接的筛选
            });
            spider.AddPipeline(new ConsolePipeline()); // 控制台打印采集结果
            spider.AddRequests("http://www.cnblogs.com/"); // 设置起始链接
            spider.RunAsync(); // 启动
        }
    }
}