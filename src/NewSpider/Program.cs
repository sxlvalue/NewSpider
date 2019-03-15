using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewSpider.Downloader;
using NewSpider.Infrastructure;
using NewSpider.MessageQueue;
using NewSpider.Scheduler;
using Serilog;
using Serilog.Events;


namespace NewSpider
{
    class Program
    {
        class MyClass
        {
            public int Id { get; set; }
            public string V { get; set; }
        }

        static void Main(string[] args)
        {
            var builder = new LocalSpiderBuilder();
            builder.UseSerilog();
            builder.UseQueueScheduler();
            var spider = builder.Build();
            spider.Id = Guid.NewGuid().ToString("N");
            spider.Name = "test";
            spider.AddRequests();
            spider.Speed = 0.5;

            for (int i = 0; i < 21; ++i)
            {
                spider.AddRequests(new Request {Url = "http://file.xbzq.ltd:5566/contents/?arg=" + i});
            }
 
            spider.RunAsync().ConfigureAwait(false);
            Console.Read();
        }
    }
}