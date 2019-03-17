using System;
using DotnetSpider.Core;

namespace DotnetSpider.Statistics
{
    public class SpiderStatistics
    {
        private readonly AtomicLong _total = new AtomicLong();
        private readonly AtomicLong _success = new AtomicLong();
        private readonly AtomicLong _failed = new AtomicLong();

        public DateTime Start { get; set; }

        public DateTime? Exit { get; set; }

        public long Total
        {
            get => _total.Value;
            set => _total.Set(value);
        }

        public long Success
        {
            get => _success.Value;
            set => _success.Set(value);
        }

        public long Failed
        {
            get => _failed.Value;
            set => _failed.Set(value);
        }

        internal void IncSuccess()
        {
            _success.Inc();
        }

        internal void IncFailed()
        {
            _failed.Inc();
        }

        internal void AddTotal(int count)
        {
            _total.Add(count);
        }
    }
}