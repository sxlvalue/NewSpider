using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Net.Http;
using DotnetSpider.Core;
using DotnetSpider.Sample.samples;

namespace DotnetSpider.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var mmf = MemoryMappedFile.CreateFromFile("test", FileMode.OpenOrCreate, null, 4,
                MemoryMappedFileAccess.ReadWrite);
            var acc = mmf.CreateViewAccessor();
            acc.Write(0, 1);
            // EntitySpider.Run();
            acc.Flush();
            var a = acc.ReadByte(0);
            // Startup.Run("-s", "CnblogsSpider", "-n", "博客园全站采集");
            Console.Read();
        }
    }
}