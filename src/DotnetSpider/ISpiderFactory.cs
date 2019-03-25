using System;
using DotnetSpider.Core;
using DotnetSpider.Statistics;

namespace DotnetSpider
{
    public interface ISpiderFactory
    {
        Spider Create();
        T Create<T>() where T : Spider;
        Spider Create(Type type);
        SpiderOptions GetOptions();
        IServiceProvider CreateScope();
        IStatisticsStore GetStatisticsStore();
    }
}