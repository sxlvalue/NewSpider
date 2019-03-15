using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewSpider.Downloader;

namespace NewSpider.Scheduler
{
    public class QueueScheduler : IScheduler
    {
        private readonly List<Request> _requests = new List<Request>();

        public Task<IEnumerable<Request>> PollAsync(string ownerId, int count)
        {
            lock (this)
            {
                var results = _requests.Take(count).ToList();
                _requests.RemoveRange(0, results.Count);
                return Task.FromResult((IEnumerable<Request>) results);
            }
        }

        public Task<uint> PushAsync(string ownerId, IEnumerable<Request> requests)
        {
            var list = requests.ToList();
            lock (this)
            {
                _requests.AddRange(list);
            }

            return Task.FromResult((uint)list.Count);
        }
    }
}