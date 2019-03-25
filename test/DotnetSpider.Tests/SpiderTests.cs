using System;
using System.Globalization;
using System.Threading;
using DotnetSpider.Core;
using DotnetSpider.Downloader;
using DotnetSpider.Scheduler;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotnetSpider.Tests
{
    public partial class SpiderTests : TestBase
    {
        [Fact(DisplayName = "RunAsyncAndStop")]
        public void RunAsyncAndStop()
        {
            var url = Environment.GetEnvironmentVariable("TRAVIS") == "1"
                ? "https://www.google.com/"
                : "http://www.baidu.com/";

            var spider = SpiderFactory.Create<Spider>();

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

            var spider = SpiderFactory.Create<Spider>();

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

            var spider = SpiderFactory.Create<Spider>();
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

        /// <summary>
        /// //TODO: 配置使用 TestDownloader, 一直抛错，检测到达指定尝试次数是否不再重试。
        /// </summary>
        [Fact(DisplayName = "RetryDownloadTimes")]
        public void RetryDownloadTimes()
        {
            var spider = SpiderFactory.Create<Spider>();
            spider.Id = Guid.NewGuid().ToString("N");
            spider.Name = "RetryDownloadTimes";
            spider.Speed = 1;
            spider.Depth = 3;
            spider.EmptySleepTime = 30;
            spider.RetryDownloadTimes = 5;
            spider.DownloaderOptions.Type = DownloaderType.Exception;
            spider.Scheduler=new QueueDistinctBfsScheduler();
            spider.AddRequests("http://www.baidu.com");
            spider.RunAsync().Wait(); // 启动

            var statisticsStore = SpiderFactory.GetStatisticsStore();
            var s = statisticsStore.GetSpiderStatisticsAsync(spider.Id).Result;
            Assert.Equal(6, s.Total);
            Assert.Equal(6, s.Failed);
            Assert.Equal(0, s.Success);

            var ds = statisticsStore.GetDownloadStatisticsListAsync(1, 10).Result[0];
            Assert.Equal(6, ds.Failed);
            Assert.Equal(0, ds.Success);
        }

        /// <summary>
        /// TODO: 当所有 DataFlow 走完的时候，如果没有任何结析结果，RetryWhenResultIsEmpty 为 True 时会把当前 Requst 添加回队列再次重试
        /// </summary>
        [Fact(DisplayName = "RetryWhenResultIsEmpty")]
        public void RetryWhenResultIsEmpty()
        {
        }

        /// <summary>
        /// TODO: 检测 Spider._speedControllerInterval 的值是否设置正确
        /// 当 Spider.Speed 设置的值 n 大于 1 时，表示每秒下载 n 个链接，因此 speed interval 设置为 1 秒， 每秒从 scheduler 中取出 n 个链接，分发到各下载器去下载。
        /// 当 Spider.Speed 设置的值 n 大于 0 小于 1 时， 表示每秒下载个数要小于 1，因此不能用 1 秒做间隔， 而应该用 1/n
        /// Spider.Speed 的值必须大于 1
        /// </summary>
        [Fact(DisplayName = "SpeedInterval")]
        public void SpeedInterval()
        {
        }

        /// <summary>
        /// TODO: 设置 Depth 为 2，使用全站采集，检测目标链接深度大于 2 的是否为入队
        /// </summary>
        [Fact(DisplayName = "Depth")]
        public void Depth()
        {
        }
    }
}