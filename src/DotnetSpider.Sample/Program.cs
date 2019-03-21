using System;
using DotnetSpider.Core;
using DotnetSpider.Data;
using DotnetSpider.Data.Storage.Model;
using DotnetSpider.Sample.samples;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetSpider.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var e = new TestModel();
            var a = e.GetTableMetadata();
            WholeSiteSpider.Run2();


            // Startup.Run("-s", "CnblogsSpider", "-n", "博客园全站采集");
            Console.Read();
        }
    }
}