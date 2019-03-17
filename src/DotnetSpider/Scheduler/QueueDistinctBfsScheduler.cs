using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DotnetSpider.Data;
using DotnetSpider.Downloader;

namespace DotnetSpider.Scheduler
{
    public class QueueDistinctBfsScheduler : DuplicateRemovedScheduler
    {
        private readonly ConcurrentDictionary<string, List<Request>> _requests =
            new ConcurrentDictionary<string, List<Request>>();

        public override void ResetDuplicateCheck()
        {
            DuplicateRemover.ResetDuplicateCheck();
        }

        protected override void PushWhenNoDuplicate(Request request)
        {
            if (!_requests.ContainsKey(request.OwnerId))
            {
                _requests.TryAdd(request.OwnerId, new List<Request>());
            }

            _requests[request.OwnerId].Add(request);
        }

        public override Request[] Dequeue(string ownerId, int count)
        {
            Check.NotNull(ownerId, nameof(ownerId));
            if (!_requests.ContainsKey(ownerId))
            {
                return new Request[0];
            }
            else
            {
                var requests = _requests[ownerId].Take(count).ToArray();
                if (requests.Length > 0)
                {
                    _requests[ownerId].RemoveRange(0, count);
                }

                return requests;
            }
        }
    }
}