using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace NewSpider.Scheduler
{
    public interface IScheduler
    {
        Task<IEnumerable<IRequest>> PollAsync(string ownerId, int count);

        Task PushAsync(string ownerId, IEnumerable<IRequest> requests);

        Task<IEnumerable<Statistics>> GetStatistics(IEnumerable<string> ownerId);

        Task IncSuccess(string ownerId);

        Task IncError(string ownerId);
    }
}