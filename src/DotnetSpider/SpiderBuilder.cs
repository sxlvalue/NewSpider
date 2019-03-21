using System;
using DotnetSpider.Core;
using DotnetSpider.Data;
using DotnetSpider.Downloader;
using DotnetSpider.Statistics;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetSpider
{
    public class SpiderBuilder
    {
        private IServiceProvider _serviceProvider;
        private bool _isRunning;
        
        public SpiderBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        public SpiderBuilder RegisterSpider(Type type)
        {
            Check.NotNull(type, nameof(type));
            Services.AddTransient(type);
            return this;
        }

        public ISpiderFactory Build()
        {
            if (_serviceProvider == null)
            {
                Services.AddTransient<Spider>();
                _serviceProvider = Services.BuildServiceProvider();
                var options = _serviceProvider.GetRequiredService<SpiderOptions>();
                if (!options.Distribute)
                {
                    StartStandaloneServices();
                }               
            }

            return new SpiderFactory(_serviceProvider);
        }

        private void StartStandaloneServices()
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
    }
}