using System;
using DotnetSpider.Core;
using DotnetSpider.Downloader;
using DotnetSpider.Downloader.Internal;
using DotnetSpider.MessageQueue;
using DotnetSpider.Scheduler;
using DotnetSpider.Statistics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace DotnetSpider
{
    public class LocalSpiderBuilder
    {
        private IServiceProvider _serviceProvider;
        private bool _isRunning;
        
        public readonly IServiceCollection Services = new ServiceCollection();
        public readonly ILoggerFactory LoggerFactory = new LoggerFactory();
        public readonly IConfigurationBuilder ConfigurationBuilder = new ConfigurationBuilder();

        public LocalSpiderBuilder()
        {
            Services.AddSingleton<IMessageQueue, LocalMessageQueue>();
            Services.AddSingleton<IDownloaderAgent, LocalDownloaderAgent>();
            Services.AddSingleton<IDownloadCenter, LocalDownloadCenter>();
            Services.AddSingleton<IDownloaderAgentStore, LocalDownloaderAgentStore>();
            Services.AddSingleton<IDownloadService, LocalDownloadCenter>();
            Services.AddSingleton<IStatisticsService, StatisticsService>();
            Services.AddSingleton<IStatisticsStore, MemoryStatisticsStore>();
            Services.AddSingleton<IStatisticsCenter, StatisticsCenter>();
            Services.AddTransient<Spider>();
        }

        public void UseConfiguration(string config = null)
        {
            ConfigurationBuilder.AddEnvironmentVariables();
            ConfigurationBuilder.AddCommandLine(Environment.GetCommandLineArgs());
            ConfigurationBuilder.AddJsonFile(string.IsNullOrWhiteSpace(config) ? "appsettings.json" : config);
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

        public Spider Build()
        {
            BuildServiceProvider();
            StartServices();
            var spider = _serviceProvider.GetService<Spider>();
            return spider;
        }

        public Spider Build(Type type)
        {
            BuildServiceProvider();
            StartServices();
            var spider = _serviceProvider.GetService(type);
            return (Spider) spider;
        }
        
        private void StartServices()
        {
            if (!_isRunning)
            {
                _serviceProvider.GetRequiredService<IDownloadCenter>().StartAsync(default)
                    .ConfigureAwait(false);
                _serviceProvider.GetRequiredService<IDownloaderAgent>().StartAsync(default)
                    .ConfigureAwait(false);
                _serviceProvider.GetRequiredService<IStatisticsCenter>().StartAsync(default)
                    .ConfigureAwait(false);
                _isRunning = true;
            }
        }

        private void BuildServiceProvider()
        {
            if (_serviceProvider == null)
            {
                Framework.SetEncoding();
                Framework.SetMultiThread();
                var configuration = ConfigurationBuilder.Build();
                Services.AddSingleton<IConfiguration>(configuration);
                Services.AddSingleton(LoggerFactory);

                _serviceProvider = Services.BuildServiceProvider();
            }
        }

        private void CheckIfBuilt()
        {
            if (_serviceProvider != null)
            {
                throw new SpiderException("构造完成后不能再修改配置");
            }
        }
    }
}