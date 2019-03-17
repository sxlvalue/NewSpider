using System.Collections.Generic;
using DotnetSpider.Downloader;

namespace DotnetSpider.Scheduler
{
    public interface IScheduler
    {
        Request[] Dequeue(string ownerId, int count);

        int Enqueue(IEnumerable<Request> requests);
    }
}