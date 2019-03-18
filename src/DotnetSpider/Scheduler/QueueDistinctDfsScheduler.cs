using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotnetSpider.Data;
using DotnetSpider.Downloader;

namespace DotnetSpider.Scheduler
{
    public class QueueDistinctDfsScheduler : DuplicateRemovedScheduler
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
                var dequeueCount = count;
                int start;
                if (_requests[ownerId].Count < count)
                {
                    dequeueCount = _requests[ownerId].Count;
                    start = 0;
                }
                else
                {
                    start = _requests[ownerId].Count - dequeueCount - 1;
                }

                var requests = new List<Request>();
                for (int i = _requests.Count - 1; i >= start; --i)
                {
                    requests.Add(_requests[ownerId][i]);
                }

                if (dequeueCount > 0)
                {
                    _requests[ownerId].RemoveRange(start, dequeueCount);
                }

                return requests.ToArray();
            }
        }
    }
}