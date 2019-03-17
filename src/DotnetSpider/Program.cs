using System;
using System.Collections.Generic;
using System.Linq;
using DotnetSpider.Downloader;

namespace DotnetSpider
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new LocalSpiderBuilder();
            builder.UseSerilog();
            builder.UseDistinctScheduler();
            var spider = builder.Build();
            spider.Id = Guid.NewGuid().ToString("N");
            spider.Name = "test";
            spider.AddRequests();
            spider.Speed = 1;

            for (int i = 0; i < 21; ++i)
            {
                spider.AddRequests(new Request {Url = "http://file.xbzq.ltd:5566/contents/?arg=" + i});
            }
 
            spider.RunAsync().ConfigureAwait(false);
            Console.Read();
        }
    }
}