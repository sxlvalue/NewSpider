using System;
using DotnetSpider.Core;

namespace DotnetSpider
{
    public interface ISpiderFactory
    {
        Spider Create();
        T Create<T>() where T : Spider;
        Spider Create(Type type);
        SpiderOptions GetOptions();
        IServiceProvider CreateScope();
    }
}