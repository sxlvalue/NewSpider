using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace DotnetSpider.Core
{
    public static class Framework
    {
        private static readonly Dictionary<string, string> SwitchMappings =
            new Dictionary<string, string>
            {
                {"-s", "spider"},
                {"-n", "name"},
                {"-i", "id"},
                {"-a", "args"},
                {"-d", "Distribute"}
            };

        public const string ResponseHandlerTopic = "ResponseHandler-";
        public const string DownloaderCenterTopic = "DownloadCenter";
        public const string StatisticsServiceTopic = "StatisticsService";

        public const string AllocateDownloaderCommand = "Allocate";
        public const string DownloadCommand = "Download";
        public const string RegisterCommand = "Register";
        public const string HeartbeatCommand = "Heartbeat";
        public const string ExitCommand = "Exit";
        public const string CommandSeparator = "|";

        public static void SetEncoding()
        {
#if NETSTANDARD
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }

        public static void SetMultiThread()
        {
            ThreadPool.SetMinThreads(256, 256);
#if !NETSTANDARD
			ServicePointManager.DefaultConnectionLimit = 1000;
#endif
        }

        public static ConfigurationBuilder CreateConfigurationBuilder(string config = null, string[] args = null)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCommandLine(Environment.GetCommandLineArgs(), SwitchMappings);
            if (args != null)
            {
                configurationBuilder.AddCommandLine(args, SwitchMappings);
            }

            configurationBuilder.AddJsonFile(string.IsNullOrWhiteSpace(config) ? "appsettings.json" : config, false,
                true);
            return configurationBuilder;
        }

        public static IConfiguration CreateConfiguration(string config=null,string[] args=null)
        {
            return CreateConfigurationBuilder(config, args).Build();
        }

        /// <summary>
        /// 打印爬虫框架信息
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void PrintInfo()
        {
            var key = "PRINT_DOTNET_SPIDER_INFO";

            var isPrinted = AppDomain.CurrentDomain.GetData(key) != null;

            if (!isPrinted)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("==================================================================");
                Console.WriteLine("== DotnetSpider is an open source crawler developed by C#       ==");
                Console.WriteLine("== It's multi thread, light weight, stable and high performance ==");
                Console.WriteLine("== Support storage data to file, mysql, mssql, mongodb etc      ==");
                Console.WriteLine("== License: MIT                                                 ==");
                Console.WriteLine("== Version: 4.0.0                                               ==");
                Console.WriteLine("== Author: zlzforever@163.com                                   ==");
                Console.WriteLine("==================================================================");
                Console.ForegroundColor = ConsoleColor.White;

                AppDomain.CurrentDomain.SetData(key, "True");
            }
        }

        /// <summary>
        /// 打印一整行word到控制台中
        /// </summary>
        /// <param name="word">打印的字符</param>
        public static void PrintLine(char word = '=')
        {
            var width = 120;

            try
            {
                width = Console.WindowWidth;
            }
            catch
            {
                // ignore
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < width; ++i)
            {
                builder.Append(word);
            }

            Console.Write(builder.ToString());
        }
    }
}