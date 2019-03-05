using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewSpider.Scheduler
{
    public class QueueScheduler : IScheduler
    {
        public Task<IEnumerable<IRequest>> PollAsync(string ownerId, uint count)
        {
            throw new System.NotImplementedException();
        }

        public Task PushAsync(string ownerId, IEnumerable<IRequest> requests)
        {
            throw new System.NotImplementedException();
        }

        public Task<IEnumerable<Statistics>> GetStatistics(IEnumerable<string> ownerId)
        {
            throw new System.NotImplementedException();
        }

        public Task IncSuccess(string ownerId)
        {
            throw new System.NotImplementedException();
        }

        public Task IncError(string ownerId)
        {
            throw new System.NotImplementedException();
        }
    }
}