using System;
using System.ComponentModel.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewSpider.Downloader;
using NewSpider.Downloader.Internal;
using NewSpider.MessageQueue;
using NewSpider.Scheduler;
using NewSpider.Statistics;
using Serilog;
using Serilog.Events;

namespace NewSpider
{
    public class LocalSpiderBuilder
    {
        private readonly IServiceCollection _services = new ServiceCollection();
        private IServiceProvider _serviceProvider;
        private readonly ILoggerFactory _loggerFactory = new LoggerFactory();
        private bool _injectedLogFactory;

        public LocalSpiderBuilder()
        {
            _services.AddSingleton<IMessageQueue, LocalMessageQueue>();
            _services.AddSingleton<IDownloaderAgent, LocalDownloaderAgent>();
            _services.AddSingleton<IDownloadCenter, LocalDownloadCenter>();
            _services.AddSingleton<IDownloaderAgentStore, LocalDownloaderAgentStore>();
            _services.AddSingleton<IDownloadService, LocalDownloadCenter>();
            _services.AddSingleton<IStatisticsStore, MemoryStatisticsStore>();
            _services.AddTransient<ISpider, Spider>();
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
                    .WriteTo.Console().WriteTo.RollingFile("new-spider.log");
            }

            Log.Logger = configuration.CreateLogger();
            _loggerFactory.AddSerilog();

            InjectLogFactory();
        }


        public void UseQueueScheduler()
        {
            CheckIfBuilt();
            _services.AddSingleton<IScheduler, QueueScheduler>();
        }

        public ISpider Build()
        {
            if (_serviceProvider == null)
            {
                _serviceProvider = _services.BuildServiceProvider();
                _serviceProvider.GetRequiredService<IDownloadCenter>().StartAsync(default)
                    .ConfigureAwait(false);
                _serviceProvider.GetRequiredService<IDownloaderAgent>().StartAsync(default)
                    .ConfigureAwait(false);
            }

            var spider = _serviceProvider.GetService<ISpider>();
            return spider;
        }

        private void CheckIfBuilt()
        {
            if (_serviceProvider != null)
            {
                throw new NewSpiderException("构造完成后不能再修改配置");
            }
        }

        private void InjectLogFactory()
        {
            if (!_injectedLogFactory)
            {
                _services.AddSingleton(_loggerFactory);
                _injectedLogFactory = true;
            }
        }
    }
}