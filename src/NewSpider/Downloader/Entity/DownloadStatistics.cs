using NewSpider.Infrastructure;

namespace NewSpider.Downloader.Entity
{
    public class DownloadStatistics
    {
        private readonly AtomicLong _elapsedMilliseconds = new AtomicLong();
        private readonly AtomicLong _success = new AtomicLong();
        private readonly AtomicLong _failed = new AtomicLong();

        public string AgentId { get; set; }

        /// <summary>
        /// 下载成功的次数
        /// </summary>
        public long Success
        {
            get => _success.Value;
            set => _success.Set(value);
        }

        /// <summary>
        /// 下载失败的次数
        /// </summary>
        public long Failed
        {
            get => _failed.Value;
            set => _failed.Set(value);
        }

        /// <summary>
        /// 每次下载所需要的时间的总和
        /// </summary>
        public long ElapsedMilliseconds
        {
            get => _elapsedMilliseconds.Value;
            set => _elapsedMilliseconds.Set(value);
        }
        
        internal void AddElapsedMilliseconds(long value)
        {
            _elapsedMilliseconds.Add(value);
        }

        internal void AddSuccess(int count)
        {
            _success.Add(count);
        }

        internal void AddFailed(int count)
        {
            _failed.Add(count);
        }
    }
}