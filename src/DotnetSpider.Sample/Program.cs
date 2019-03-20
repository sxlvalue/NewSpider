using System;
using DotnetSpider.Core;
using DotnetSpider.Data;
using DotnetSpider.Downloader;
using DotnetSpider.Sample.samples;

namespace DotnetSpider.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Startup.Run(args);
            Console.Read();
        }
    }
}