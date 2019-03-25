using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DotnetSpider.Data;
using DotnetSpider.Downloader;

namespace DotnetSpider.Scheduler
{
    public class QueueDistinctBfsScheduler : DuplicateRemovedScheduler
    {
        internal readonly ConcurrentDictionary<string, List<Request>> Requests =
            new ConcurrentDictionary<string, List<Request>>();

        public override void ResetDuplicateCheck()
        {
            DuplicateRemover.ResetDuplicateCheck();
        }

        protected override void PushWhenNoDuplicate(Request request)
        {
            if (!Requests.ContainsKey(request.OwnerId))
            {
                Requests.TryAdd(request.OwnerId, new List<Request>());
            }

            Requests[request.OwnerId].Add(request);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override Request[] Dequeue(string ownerId, int count = 1)
        {
            Check.NotNull(ownerId, nameof(ownerId));
            if (!Requests.ContainsKey(ownerId))
            {
                return new Request[0];
            }

            var requests = Requests[ownerId].Take(count).ToArray();
            if (requests.Length > 0)
            {
                Requests[ownerId].RemoveRange(0, requests.Length);
            }

            return requests;
        }
    }
}