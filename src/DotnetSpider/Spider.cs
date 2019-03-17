using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetSpider.Core;
using DotnetSpider.Data;
using DotnetSpider.Downloader;
using DotnetSpider.Downloader.Entity;
using DotnetSpider.MessageQueue;
using DotnetSpider.Scheduler;
using DotnetSpider.Statistics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DotnetSpider
{
    public partial class Spider : ISpider
    {
        private const int ProcessComparer = 0;
        private const int PipelineComparer = 1;
        private readonly IList<Request> _requests = new List<Request>();

        private readonly List<IDataFlow> _dataFlows = new List<IDataFlow>();
        private readonly IMessageQueue _mq;
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;
        private readonly IDownloadService _downloadService;
        private readonly IStatisticsStore _statisticsService;
        private DateTime _lastRequestedTime;

        private Status _status;
        private int _emptySleepTime = 30;
        private int _retryDownloadTimes = 5;
        private int _statisticsInterval = 5;
        private double _speed;
        private int _speedControllerInterval = 1000;
        private int _dequeueBatchCount = 1;

        public event RequestHandler PretreatmentDownload;

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
                    throw new DotnetSpiderException("下载速度必须大于 0");
                }

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
        /// <exception cref="DotnetSpiderException"></exception>
        public int StatisticsInterval
        {
            get => _statisticsInterval;
            set
            {
                if (value < 5)
                {
                    throw new DotnetSpiderException("上报状态间隔必须大于 5 (秒)");
                }

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
                    throw new DotnetSpiderException("下载重试次数必须大于 0");
                }

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
                    throw new DotnetSpiderException($"等待结束时间必需大于速度控制器间隔: {_speedControllerInterval}");
                }

                if (value < 30)
                {
                    throw new DotnetSpiderException("等待结束时间必需大于 30 (秒)");
                }

                _emptySleepTime = value;
            }
        }

        public Spider(IMessageQueue mq,
            IDownloadService downloadService, IStatisticsStore statisticsService, IScheduler scheduler,
            ILoggerFactory loggerFactory)
        {
            _downloadService = downloadService;
            _scheduler = scheduler;
            _statisticsService = statisticsService;
            _mq = mq;

            _logger = loggerFactory.CreateLogger(typeof(Spider).Name);
        }

        public Task RunAsync()
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    _status = Status.Running;
                    // 添加任务启动的监控信息
                    await _statisticsService.StartAsync(Id);

                    PushRequests();

                    // 数据处理流程排序
                    _dataFlows.Sort();

                    // 分配下载器: 可以通过消息队列(本地模式)或者HTTP接口(配合Portal)分配
                    var allocated = await AllotDownloaderAsync();
                    if (!allocated)
                    {
                        return;
                    }

                    _logger.LogInformation($"任务 {Id} 分配下载器成功");

                    // 启动速度控制器
                    StartSpeedControllerAsync().ConfigureAwait(false);

                    // 订阅数据流
                    _mq.Subscribe($"{DotnetSpiderConsts.ResponseHandlerTopic}{Id}",
                        async message => await HandleMessage(message));

                    _lastRequestedTime = DateTime.Now;

                    await WaitForExit();

                    _logger.LogInformation($"任务 {Id} 退出");
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                }
                finally
                {
                    _status = Status.Exited;
                    // 添加任务退出的监控信息
                    await _statisticsService.ExitAsync(Id);

                    await PrintStatistics();
                }
            });
        }

        public void Pause()
        {
            _status = Status.Paused;
        }

        public void Continue()
        {
            _status = Status.Running;
        }

        public void Exit()
        {
            _status = Status.Exited;
            // 直接取消订阅即可: 1. 如果是本地应用, 
            _mq.Unsubscribe($"{DotnetSpiderConsts.ResponseHandlerTopic}{Id}");
        }

        private async Task<bool> AllotDownloaderAsync()
        {
            return await _downloadService.AllocateAsync(new AllotDownloaderMessage
            {
                OwnerId = Id,
                Type = DownloaderType.Sample,
                Speed = Speed,
                UseProxy = false,
                DownloaderCount = DownloaderCount
            });
        }

        private async Task HandleMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning($"任务 {Id} 接收到空消息");
                return;
            }

            _lastRequestedTime = DateTime.Now;
            var responses = JsonConvert.DeserializeObject<List<Response>>(message);

            if (responses.Count == 0)
            {
                _logger.LogWarning($"任务 {Id} 接收到空下载内容");
                return;
            }

            var agentId = responses.First().AgentId;

            var successResponses = responses.Where(x => x.Success).ToList();
            // 统计下载成功
            if (successResponses.Count > 0)
            {
                var elapsedMilliseconds = successResponses.Sum(x => x.ElapsedMilliseconds);
                await _statisticsService.IncrementDownloadSuccessAsync(agentId, successResponses.Count,
                    elapsedMilliseconds);
            }

            // 处理下载成功的请求
            Parallel.ForEach(successResponses, async (response) =>
            {
                _logger.LogInformation($"任务 {Id} 下载 {response.Request.Url} 成功");

                var context = new DataFlowContext();
                context.AddRequest(response.Request);
                try
                {
                    bool success = true;
                    foreach (var dataFlow in _dataFlows)
                    {
                        success = await dataFlow.Handle(context);
                        if (!success)
                        {
                            break;
                        }
                    }

                    // 解析的目标请求
                    if (context.Properties != null && context.Properties.ContainsKey("ExtractedRequests"))
                    {
                        var count = _scheduler.Enqueue(context.Properties["ExtractedRequests"]);
                        await _statisticsService.IncrementTotalAsync(Id, count);
                    }

                    if (success)
                    {
                        await _statisticsService.IncrementSuccessAsync(Id);
                    }
                    else
                    {
                        await _statisticsService.IncrementFailedAsync(Id);
                    }

                    var result = success ? "成功" : $"失败: {context.Result}";
                    _logger.LogInformation($"任务 {Id} 处理 {response.Request.Url} {result}");
                }
                catch (Exception e)
                {
                    _logger.LogInformation($"任务 {Id} 处理 {response.Request.Url} 失败: {e}");
                }
            });

            var retryResponses =
                responses.Where(x => !x.Success && x.Request.RetriedTimes < RetryDownloadTimes)
                    .ToList();

            retryResponses.ForEach(x =>
            {
                x.Request.RetriedTimes++;
                _logger.LogInformation($"任务 {Id} 下载 {x.Request.Url} 失败: {x.Exception}");
            });

            var failedRequests =
                responses.Where(x => !x.Success && x.Request.RetriedTimes >= RetryDownloadTimes)
                    .ToList();
            // 统计下载失败
            if (failedRequests.Count > 0)
            {
                await _statisticsService.IncrementDownloadFailedAsync(agentId, failedRequests.Count);
            }

            _scheduler.Enqueue(retryResponses.Select(x => x.Request));
        }

        private async Task WaitForExit()
        {
            int waited = 0;
            while (_status != Status.Exited)
            {
                if ((DateTime.Now - _lastRequestedTime).Seconds > EmptySleepTime)
                {
                    Exit();
                }

                Thread.Sleep(1000);
                waited += 1;
                if (waited > StatisticsInterval)
                {
                    waited = 0;
                    await PrintStatistics();
                }
            }
        }

        private async Task PrintStatistics()
        {
            var statistics = await _statisticsService.GetSpiderStatisticsAsync(Id);
            _logger.LogTrace(
                $"任务 {Id} 总计 {statistics.Total}, 成功 {statistics.Success}, 失败 {statistics.Failed}, 剩余 {(statistics.Total - statistics.Success - statistics.Failed)}");
        }
    }
}