using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewSpider.Downloader;
using NewSpider.Scheduler;
using Serilog;
using Serilog.Events;


namespace NewSpider
{
    class Program
    {
        static void Main(string[] args)
        {
            Infrastructure.Log.UseSerilog();
            var spider = new Spider(Guid.NewGuid().ToString("N"), "test");
            for (int i = 0; i < 1100; ++i)
            {
                spider.AddRequest(new Request {Url = "http://file.xbzq.ltd:5566/contents/?arg=" + i});
            }

            spider.RunAsync().ConfigureAwait(false);
            Console.Read();
        }
    }
}