using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NewSpider.Downloader;

namespace NewSpider.Scheduler
{
    public interface IScheduler
    {
        Task<IEnumerable<Request>> PollAsync(string ownerId, int count);

        Task<uint> PushAsync(string ownerId, IEnumerable<Request> requests);
    }
}