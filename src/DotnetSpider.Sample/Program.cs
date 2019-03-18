using System;
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