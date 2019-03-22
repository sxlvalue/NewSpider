using System;
using DotnetSpider.Sample.samples;

namespace DotnetSpider.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            EntitySpider.Run();

            // Startup.Run("-s", "CnblogsSpider", "-n", "博客园全站采集");
            Console.Read();
        }
    }
}