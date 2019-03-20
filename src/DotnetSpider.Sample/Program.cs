using System;
using DotnetSpider.Data;
using DotnetSpider.Downloader;
using DotnetSpider.Sample.samples;

namespace DotnetSpider.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            WholeSiteSpider.Run2();
            Console.Read();
        }
    }
}