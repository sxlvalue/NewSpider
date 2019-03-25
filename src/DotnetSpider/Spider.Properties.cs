using System;
using System.Collections.Generic;
using DotnetSpider.Core;
using DotnetSpider.Data;
using DotnetSpider.Downloader;
using DotnetSpider.MessageQueue;
using DotnetSpider.RequestSupply;
using DotnetSpider.Scheduler;
using DotnetSpider.Statistics;
using Microsoft.Extensions.Logging;

namespace DotnetSpider
{
    public partial class Spider
    {
        private readonly IList<Request> _requests = new List<Request>();

        private readonly List<IDataFlow> _dataFlows = new List<IDataFlow>();
        private readonly IMessageQueue _mq;
        private readonly ILogger _logger;
        private readonly IDownloadService _downloadService;
        private readonly IStatisticsService _statisticsService;
        private readonly ILoggerFactory _loggerFactory;
        private readonly List<IRequestSupply> _requestSupplies = new List<IRequestSupply>();

        /// <summary>
        /// Cookie Container
        /// </summary>
        /// <summary xml:lang="zh-CN">
        /// Cookie 管理容器
        /// </summary>
        private readonly HashSet<Cookie> _cookies = new HashSet<Cookie>();

        private DateTime _lastRequestedTime;
        private Status _status;
        private IScheduler _scheduler;
        private int _emptySleepTime = 30;
        private int _retryDownloadTimes = 5;
        private int _statisticsInterval = 5;
        private double _speed;
        private int _speedControllerInterval = 1000;
        private int _dequeueBatchCount = 1;
        private int _depth = int.MaxValue;

        public event Action<Request> OnDownloading;

        public DownloaderType DownloaderType { get; set; } = DownloaderType.Default;

        /// <summary>
        /// 遍历深度
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public int Depth
        {
            get => _depth;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("遍历深度必须大于 0");
                }

                CheckIfRunning();
                _depth = value;
            }
        }

        public IScheduler Scheduler
        {
            get => _scheduler;
            set
            {
                CheckIfRunning();
                _scheduler = value;
            }
        }

        /// <summary>
        /// 每秒尝试下载多少个请求
        /// </summary>
        public double Speed
        {
            get => _speed;
            set
            {
                if (value <= 0)
                {
                    throw new SpiderException("下载速度必须大于 0");
                }

                CheckIfRunning();

                _speed = value;

                if (_speed >= 1)
                {
                    _speedControllerInterval = 1000;
                    _dequeueBatchCount = (int) _speed;
                }
                else
                {
                    _speedControllerInterval = (int) (1 / _speed) * 1000;
                    _dequeueBatchCount = 1;
                }

                var maybeEmptySleepTime = _speedControllerInterval / 1000;
                if (maybeEmptySleepTime >= EmptySleepTime)
                {
                    var larger = (int) (maybeEmptySleepTime * 1.5);
                    EmptySleepTime = larger > 30 ? larger : 30;
                }
            }
        }

        public int EnqueueBatchCount { get; set; } = 1000;

        /// <summary>
        /// 上报状态的间隔时间，单位: 秒
        /// </summary>
        /// <exception cref="SpiderException"></exception>
        public int StatisticsInterval
        {
            get => _statisticsInterval;
            set
            {
                if (value < 5)
                {
                    throw new SpiderException("上报状态间隔必须大于 5 (秒)");
                }

                CheckIfRunning();
                _statisticsInterval = value;
            }
        }

        public int DownloaderCount { get; set; } = 1;

        public string Id { get; set; }

        public string Name { get; set; }


        public int RetryDownloadTimes
        {
            get => _retryDownloadTimes;
            set
            {
                if (value <= 0)
                {
                    throw new SpiderException("下载重试次数必须大于 0");
                }

                CheckIfRunning();
                _retryDownloadTimes = value;
            }
        }

        public int EmptySleepTime
        {
            get => _emptySleepTime;
            set
            {
                if (value <= _speedControllerInterval)
                {
                    throw new SpiderException($"等待结束时间必需大于速度控制器间隔: {_speedControllerInterval}");
                }

                if (value < 30)
                {
                    throw new SpiderException("等待结束时间必需大于 30 (秒)");
                }

                CheckIfRunning();
                _emptySleepTime = value;
            }
        }

        /// <summary>
        /// Add one cookie to downloader
        /// </summary>
        /// <summary xml:lang="zh-CN">
        /// 添加Cookie
        /// </summary>
        /// <param name="name">名称(<see cref="Cookie.Name"/>)</param>
        /// <param name="value">值(<see cref="Cookie.Value"/>)</param>
        /// <param name="domain">作用域(<see cref="Cookie.Domain"/>)</param>
        /// <param name="path">作用路径(<see cref="Cookie.Path"/>)</param>
        public void AddCookie(string name, string value, string domain, string path = "/")
        {
            var cookie = new Cookie(name, value, domain, path);
            AddCookie(cookie);
        }

        /// <summary>
        /// Add cookies to downloader
        /// </summary>
        /// <summary xml:lang="zh-CN">
        /// 添加Cookies
        /// </summary>
        /// <param name="cookies">Cookies的键值对 (Cookie's key-value pairs)</param>
        /// <param name="domain">作用域(<see cref="Cookie.Domain"/>)</param>
        /// <param name="path">作用路径(<see cref="Cookie.Path"/>)</param>
        public void AddCookies(IDictionary<string, string> cookies, string domain, string path = "/")
        {
            foreach (var pair in cookies)
            {
                var name = pair.Key;
                var value = pair.Value;
                AddCookie(name, value, domain, path);
            }
        }

        /// <summary>
        /// Add cookies to downloader
        /// </summary>
        /// <summary xml:lang="zh-CN">
        /// 设置 Cookies
        /// </summary>
        /// <param name="cookies">Cookies的键值对字符串, 如: a1=b;a2=c;(Cookie's key-value pairs string, a1=b;a2=c; etc.)</param>
        /// <param name="domain">作用域(<see cref="Cookie.Domain"/>)</param>
        /// <param name="path">作用路径(<see cref="Cookie.Path"/>)</param>
        public void AddCookies(string cookies, string domain, string path = "/")
        {
            var pairs = cookies.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);
                var name = keyValue[0];
                string value = keyValue.Length > 1 ? keyValue[1] : string.Empty;
                AddCookie(name, value, domain, path);
            }
        }

        /// <summary>
        /// Add one cookie to downloader
        /// </summary>
        /// <summary xml:lang="zh-CN">
        /// 设置 Cookie
        /// </summary>
        /// <param name="cookie">Cookie</param>
        public virtual void AddCookie(Cookie cookie)
        {
            _cookies.Add(cookie);
        }
    }
}