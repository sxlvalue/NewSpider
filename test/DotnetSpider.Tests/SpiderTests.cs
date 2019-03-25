using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using DotnetSpider.Downloader;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotnetSpider.Tests
{
    public partial class SpiderTests
    {
        [Fact(DisplayName = "RunAsyncAndStop")]
        public void RunAsyncAndStop()
        {
            var url = Environment.GetEnvironmentVariable("TRAVIS") == "1"
                ? "https://www.google.com/"
                : "http://www.baidu.com/";

            var services = new ServiceCollection();
            services.AddDotnetSpider(builder =>
            {
                builder.UseConfiguration();
                builder.UseSerilog();
                builder.UseStandalone();
            });
            var factory = services.BuildServiceProvider().GetRequiredService<ISpiderFactory>();
            var spider = factory.Create<Spider>();

            spider.Id = Guid.NewGuid().ToString("N"); // 设置任务标识
            spider.Name = "RunAsyncAndStop"; // 设置任务名称
            spider.Speed = 1; // 设置采集速度, 表示每秒下载多少个请求, 大于 1 时越大速度越快, 小于 1 时越小越慢, 不能为0.
            spider.Depth = 3; // 设置采集深度
            spider.DownloaderOptions.Type = DownloaderType.HttpClient; // 使用普通下载器, 无关 Cookie, 干净的 HttpClient 
  
            for (int i = 0; i < 10000; i++)
            {
                spider.AddRequests(new Request(url + i) {Encoding = "UTF-8"});
            }

            spider.RunAsync();
            Thread.Sleep(2000);
            spider.Pause();
            Thread.Sleep(2000);
            spider.Exit();

            for (int i = 0; i < 50; ++i)
            {
                if (spider.Status == Status.Exited)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }

            Assert.Equal(Status.Exited, spider.Status);
        }

        [Fact(DisplayName = "RunAsyncAndContinue")]
        public void RunAsyncAndContinue()
        {
            var url = Environment.GetEnvironmentVariable("TRAVIS") == "1"
                ? "https://www.google.com/"
                : "http://www.baidu.com/";

            var services = new ServiceCollection();
            services.AddDotnetSpider(builder =>
            {
                builder.UseConfiguration();
                builder.UseSerilog();
                builder.UseStandalone();
            });
            var factory = services.BuildServiceProvider().GetRequiredService<ISpiderFactory>();
            var spider = factory.Create<Spider>();

            spider.Id = Guid.NewGuid().ToString("N"); // 设置任务标识
            spider.Name = "RunAsyncAndStop"; // 设置任务名称
            spider.Speed = 1; // 设置采集速度, 表示每秒下载多少个请求, 大于 1 时越大速度越快, 小于 1 时越小越慢, 不能为0.
            spider.Depth = 3; // 设置采集深度
            spider.DownloaderOptions.Type = DownloaderType.HttpClient; // 使用普通下载器, 无关 Cookie, 干净的 HttpClient 
 
            for (int i = 0; i < 10000; i++)
            {
                spider.AddRequests(new Request(url + i) {Encoding = "UTF-8"});
            }

            spider.RunAsync();
            Thread.Sleep(4000);
            spider.Pause();
            Thread.Sleep(4000);
            spider.Continue();
            Thread.Sleep(4000);
            spider.Exit();

            for (int i = 0; i < 50; ++i)
            {
                if (spider.Status == Status.Exited)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }

            Assert.Equal(Status.Exited, spider.Status);
        }


        [Fact(DisplayName = "CloseSignal")]
        public void CloseSignal()
        {
            var url = Environment.GetEnvironmentVariable("TRAVIS") == "1"
                ? "https://www.google.com/"
                : "http://www.baidu.com/";

            var services = new ServiceCollection();
            services.AddDotnetSpider(builder =>
            {
                builder.UseConfiguration();
                builder.UseSerilog();
                builder.UseStandalone();
            });
            var factory = services.BuildServiceProvider().GetRequiredService<ISpiderFactory>();
            var spider = factory.Create<Spider>();
            spider.MmfSignal = true;
            spider.Id = Guid.NewGuid().ToString("N"); // 设置任务标识
            spider.Name = "RunAsyncAndStop"; // 设置任务名称
            spider.Speed = 1; // 设置采集速度, 表示每秒下载多少个请求, 大于 1 时越大速度越快, 小于 1 时越小越慢, 不能为0.
            spider.Depth = 3; // 设置采集深度
            spider.DownloaderOptions.Type = DownloaderType.HttpClient; // 使用普通下载器, 无关 Cookie, 干净的 HttpClient 
  
            for (int i = 0; i < 10000; i++)
            {
                spider.AddRequests(new Request(url + i) {Encoding = "UTF-8"});
            }

            spider.RunAsync(); // 启动
            Thread.Sleep(4000);
            spider.ExitBySignal();

            for (int i = 0; i < 50; ++i)
            {
                if (spider.Status == Status.Exited)
                {
                    break;
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }

            Assert.Equal(Status.Exited, spider.Status);
        }
    }
}