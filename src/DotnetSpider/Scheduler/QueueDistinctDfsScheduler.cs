using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotnetSpider.Data;
using DotnetSpider.Downloader;

namespace DotnetSpider.Scheduler
{
    public class QueueDistinctDfsScheduler : DuplicateRemovedScheduler
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

            var dequeueCount = count;
            int start;
            if (Requests[ownerId].Count < count)
            {
                dequeueCount = Requests[ownerId].Count;
                start = 0;
            }
            else
            {
                start = Requests[ownerId].Count - dequeueCount;
            }

            var requests = new List<Request>();
            for (int i = Requests.Count; i >= start; --i)
            {
                requests.Add(Requests[ownerId][i]);
            }

            if (dequeueCount > 0)
            {
                Requests[ownerId].RemoveRange(start, dequeueCount);
            }

            return requests.ToArray();
        }
    }
}