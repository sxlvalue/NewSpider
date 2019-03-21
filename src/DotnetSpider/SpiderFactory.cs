using System;
using DotnetSpider.Core;
using DotnetSpider.Data;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetSpider
{
    public class SpiderFactory : ISpiderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SpiderFactory(IServiceProvider serviceProvider)
        {
            Check.NotNull(serviceProvider, nameof(serviceProvider));
            _serviceProvider = serviceProvider;
        }


        public Spider Create()
        {
            return _serviceProvider.GetRequiredService<Spider>();
        }

        public T Create<T>() where T : Spider
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        public Spider Create(Type type)
        {
            var spiderType = typeof(Spider);
            if (!spiderType.IsAssignableFrom(type))
            {
                throw new SpiderException($"类型 {type} 不是爬虫类型");
            }

            return (Spider) _serviceProvider.GetRequiredService(type);
        }
    }
}