using System;
using DotnetSpider.Data;
using DotnetSpider.Data.Pipeline;
using DotnetSpider.Data.Processor;
using DotnetSpider.Downloader;
using DotnetSpider.Sample.samples;

namespace DotnetSpider.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            CrawlerWholeSiteSpider.Run();
            Console.Read();
        }
    }
}