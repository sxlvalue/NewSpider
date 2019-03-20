using System;
using DotnetSpider.Core;

namespace DotnetSpider.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Startup.Run(new[] {"-s", "CnblogsSpider", "-n", "博客园全站采集"});
            Console.Read();
        }
    }
}