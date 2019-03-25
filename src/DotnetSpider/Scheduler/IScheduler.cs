using System.Collections.Generic;
using DotnetSpider.Downloader;

namespace DotnetSpider.Scheduler
{
    public interface IScheduler
    {
        Request[] Dequeue(string ownerId, int count = 1);

        int Enqueue(IEnumerable<Request> requests);

        int Total { get; }
    }
}