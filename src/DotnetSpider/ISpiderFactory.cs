using System;

namespace DotnetSpider
{
    public interface ISpiderFactory
    {
        Spider Create();
        T Create<T>() where T : Spider;
        Spider Create(Type type);
    }
}