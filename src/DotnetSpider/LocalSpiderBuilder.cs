using System;
using DotnetSpider.Core;
using DotnetSpider.Downloader;
using DotnetSpider.Downloader.Internal;
using DotnetSpider.MessageQueue;
using DotnetSpider.Scheduler;
using DotnetSpider.Statistics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace DotnetSpider
{
    public class LocalSpiderBuilder : ISpiderBuilder
    {
        private IServiceProvider _serviceProvider;

        public readonly IServiceCollection Services = new ServiceCollection();
        public readonly ILoggerFactory LoggerFactory = new LoggerFactory();

        public LocalSpiderBuilder()
        {
            Services.AddSingleton<IMessageQueue, LocalMessageQueue>();
            Services.AddSingleton<IDownloaderAgent, LocalDownloaderAgent>();
            Services.AddSingleton<IDownloadCenter, LocalDownloadCenter>();
            Services.AddSingleton<IDownloaderAgentStore, LocalDownloaderAgentStore>();
            Services.AddSingleton<IDownloadService, LocalDownloadCenter>();
            Services.AddSingleton<IStatisticsStore, MemoryStatisticsStore>();
            Services.AddTransient<ISpider, Spider>();
        }

        public void UseSerilog(LoggerConfiguration configuration = null)
        {
            CheckIfBuilt();

            if (configuration == null)
            {
                configuration = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console().WriteTo.RollingFile("dotnet-spider.log");
            }

            Log.Logger = configuration.CreateLogger();
            LoggerFactory.AddSerilog();
        }


        public void UseQueueScheduler(TraverseStrategy traverseStrategy = TraverseStrategy.Bfs)
        {
            CheckIfBuilt();
            if (traverseStrategy == TraverseStrategy.Bfs)
            {
                Services.AddSingleton<IScheduler, QueueBfsScheduler>();
            }

            if (traverseStrategy == TraverseStrategy.Dfs)
            {
                Services.AddSingleton<IScheduler, QueueDfsScheduler>();
            }
        }

        public void UseDistinctScheduler(TraverseStrategy traverseStrategy = TraverseStrategy.Bfs)
        {
            CheckIfBuilt();
            if (traverseStrategy == TraverseStrategy.Bfs)
            {
                Services.AddSingleton<IScheduler, QueueDistinctBfsScheduler>();
            }

            if (traverseStrategy == TraverseStrategy.Dfs)
            {
                Services.AddSingleton<IScheduler, QueueDistinctDfsScheduler>();
            }
        }

        public Spider Build()
        {
            if (_serviceProvider == null)
            {
                Services.AddSingleton(LoggerFactory);
                _serviceProvider = Services.BuildServiceProvider();

                _serviceProvider.GetRequiredService<IDownloadCenter>().StartAsync(default)
                    .ConfigureAwait(false);
                _serviceProvider.GetRequiredService<IDownloaderAgent>().StartAsync(default)
                    .ConfigureAwait(false);
            }

            var spider = _serviceProvider.GetService<ISpider>();
            return (Spider) spider;
        }

        private void CheckIfBuilt()
        {
            if (_serviceProvider != null)
            {
                throw new DotnetSpiderException("构造完成后不能再修改配置");
            }
        }
    }
}