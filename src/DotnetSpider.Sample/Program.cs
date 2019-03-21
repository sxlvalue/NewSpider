using System;
using DotnetSpider.Core;
using DotnetSpider.Sample.samples;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetSpider.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            WholeSiteSpider.Run2();

            // Startup.Run("-s", "CnblogsSpider", "-n", "博客园全站采集");
            Console.Read();
        }
    }
}