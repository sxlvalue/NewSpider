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
//            Infrastructure.Log.UseSerilog();
//            var spider = new Spider(Guid.NewGuid().ToString("N"), "test");
//            spider.Speed = 5;
//            for (int i = 0; i < 1100; ++i)
//            {
//                spider.AddRequest(new Request {Url = "http://file.xbzq.ltd:5566/contents/?arg=" + i});
//            }
//
//            spider.RunAsync().ConfigureAwait(false);

            var list = new List<MyClass>
            {
                new MyClass {Id = 1, V = "a"}, new MyClass {Id = 1, V = "b"}, new MyClass {Id = 1, V = "c"},
                new MyClass {Id = 2, V = "d"}
            };
            var a = list.GroupBy(x => x.Id).ToDictionary(x=>x.Key,y=>y.ToList());
            Console.Read();
        }
    }
}