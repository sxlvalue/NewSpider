using System;
using DotnetSpider.Downloader;

namespace DotnetSpider.RequestSupply
{
    public interface IRequestSupply
    {
       void Run(Action<Request> enqueueAction);
    }
}