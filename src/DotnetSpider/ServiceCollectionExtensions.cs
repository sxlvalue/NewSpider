using System;
using DotnetSpider.Core;
using DotnetSpider.Data;
using DotnetSpider.Downloader;
using DotnetSpider.Downloader.Internal;
using DotnetSpider.MessageQueue;
using DotnetSpider.Statistics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.AspNetCore;
using Serilog.Events;

namespace DotnetSpider
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDotnetSpider(this IServiceCollection services,
            Action<SpiderBuilder> configureBuilder = null)
        {
            SpiderBuilder builder = new SpiderBuilder(services);
            configureBuilder?.Invoke(builder);
            services.AddSingleton(builder);
            services.AddScoped<SpiderOptions>();
            return services;
        }

        public static SpiderBuilder UseConfiguration(this SpiderBuilder builder, string config = null,
            string[] args = null)
        {
            var configurationBuilder = Framework.CreateConfigurationBuilder(config, args);

            builder.Services.AddSingleton<IConfigurationBuilder>(configurationBuilder);
            builder.Services.AddScoped<IConfiguration>(provider =>
                provider.GetRequiredService<IConfigurationBuilder>().Build());
            return builder;
        }

        public static SpiderBuilder UseSerilog(this SpiderBuilder builder, LoggerConfiguration configuration = null)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddSingleton<ILoggerFactory, SerilogLoggerFactory>();

            if (configuration == null)
            {
                configuration = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console().WriteTo.RollingFile("dotnet-spider.log");
            }

            Log.Logger = configuration.CreateLogger();

            return builder;
        }

        /// <summary>
        /// 单机模式
        /// 在单机模式下，使用内存型消息队列，因此只有在此作用域 SpiderBuilder 下构建的的爬虫才会共用一个消息队列。
        /// </summary>
        /// <param name="builder">爬虫构造器</param>
        /// <returns>爬虫构造器</returns>
        public static SpiderBuilder UseStandalone(this SpiderBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));

            builder.Services.AddSingleton<IMessageQueue, LocalMessageQueue>();
            builder.Services.AddSingleton<IDownloaderAgent, LocalDownloaderAgent>();
            builder.Services.AddSingleton<IDownloadCenter, LocalDownloadCenter>();
            builder.Services.AddSingleton<IDownloaderAgentStore, LocalDownloaderAgentStore>();
            builder.Services.AddSingleton<IDownloadService, LocalDownloadCenter>();
            builder.Services.AddSingleton<IStatisticsService, StatisticsService>();
            builder.Services.AddSingleton<IStatisticsStore, MemoryStatisticsStore>();
            builder.Services.AddSingleton<IStatisticsCenter, StatisticsCenter>();

            return builder;
        }
    }
}