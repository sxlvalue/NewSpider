using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewSpider.Scheduler
{
    public class QueueScheduler : IScheduler
    {
        private readonly List<IRequest> _requests = new List<IRequest>();

        public Task<IEnumerable<IRequest>> PollAsync(string ownerId, int count)
        {
            lock (this)
            {
                return Task.FromResult(_requests.Take(count));
            }
        }

        public Task PushAsync(string ownerId, IEnumerable<IRequest> requests)
        {
            lock (this)
            {
                _requests.AddRange(requests);
            }

            return Task.CompletedTask;
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