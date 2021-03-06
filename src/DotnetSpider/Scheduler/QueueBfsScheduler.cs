using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DotnetSpider.Data;
using DotnetSpider.Downloader;

namespace DotnetSpider.Scheduler
{
    public class QueueBfsScheduler : DuplicateRemovedScheduler
    {
        private readonly ConcurrentDictionary<string, List<Request>> _requests =
            new ConcurrentDictionary<string, List<Request>>();

        public QueueBfsScheduler()
        {
            DuplicateRemover = new FakeDuplicateRemover();
        }

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

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override Request[] Dequeue(string ownerId, int count = 1)
        {
            Check.NotNull(ownerId, nameof(ownerId));
            if (!_requests.ContainsKey(ownerId))
            {
                return new Request[0];
            }

            var requests = _requests[ownerId].Take(count).ToArray();
            if (requests.Length > 0)
            {
                _requests[ownerId].RemoveRange(0, count);
            }

            return requests;
        }
    }
}