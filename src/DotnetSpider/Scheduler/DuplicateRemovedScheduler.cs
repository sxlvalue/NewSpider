using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotnetSpider.Downloader;
using DotnetSpider.Scheduler.Component;

namespace DotnetSpider.Scheduler
{
    public abstract class DuplicateRemovedScheduler : IScheduler
    {
        /// <summary>
        /// Reset duplicate check.
        /// </summary>
        public abstract void ResetDuplicateCheck();

        /// <summary>
        /// 如果链接不是重复的就添加到队列中
        /// </summary>
        /// <param name="request">请求对象</param>
        protected abstract void PushWhenNoDuplicate(Request request);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            DuplicateRemover?.Dispose();
        }

        /// <summary>
        /// 去重器
        /// </summary>
        protected IDuplicateRemover DuplicateRemover { get; set; } = new HashSetDuplicateRemover();

        public int Total => DuplicateRemover.Total;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public abstract Request[] Dequeue(string ownerId, int count = 1);

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int Enqueue(IEnumerable<Request> requests)
        {
            int count = 0;
            foreach (var request in requests)
            {
                request.ComputeHash();
                if (!DuplicateRemover.IsDuplicate(request))
                {
                    PushWhenNoDuplicate(request);
                    count++;
                }
            }

            return count;
        }
    }
}