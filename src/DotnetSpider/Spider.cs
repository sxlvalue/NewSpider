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
        private const int DataParserComparer = 10;
        private const int StorageComparer = 20;
        private readonly IList<Request> _requests = new List<Request>();

        private readonly List<IDataFlow> _dataFlows = new List<IDataFlow>();
        private readonly IMessageQueue _mq;
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;
        private readonly IDownloadService _downloadService;
        private readonly IStatisticsService _statisticsService;
        private readonly ILoggerFactory _loggerFactory;
        private DateTime _lastRequestedTime;

        private Status _status;
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

                _depth = value;
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

        public string Cookie { get; set; }


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
            IDownloadService downloadService, IStatisticsService statisticsService, IScheduler scheduler,
            ILoggerFactory loggerFactory)
        {
            _downloadService = downloadService;
            _scheduler = scheduler;
            _statisticsService = statisticsService;
            _mq = mq;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(typeof(Spider).Name);
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

                    EnqueueRequests();

                    // 数据处理流程排序
                    _dataFlows.Sort(new DataFlowComparer());

                    // 分配下载器: 可以通过消息队列(本地模式)或者HTTP接口(配合Portal)分配
                    var allocated = await AllotDownloaderAsync();
                    if (!allocated)
                    {
                        return;
                    }

                    _logger.LogInformation($"任务 {Id} 分配下载器成功");

                    // 启动速度控制器
                    StartSpeedControllerAsync().ConfigureAwait(false).GetAwaiter();

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

                    await _statisticsService.PrintStatisticsAsync(Id);
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
                Type = DownloaderType,
                Speed = Speed,
                UseProxy = false,
                DownloaderCount = DownloaderCount,
                Cookie = Cookie
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
                _logger.LogWarning($"任务 {Id} 接收到空回复");
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
                context.AddResponse(response);
                try
                {
                    bool success = true;
                    foreach (var dataFlow in _dataFlows)
                    {
                        var dataFlowResult = await dataFlow.Handle(context);
                        switch (dataFlowResult)
                        {
                            case DataFlowResult.Success:
                            {
                                continue;
                            }
                            case DataFlowResult.Failed:
                            {
                                success = false;
                                break;
                            }
                            case DataFlowResult.Terminated:
                            {
                                break;
                            }
                        }
                    }

                    // 解析的目标请求
                    var targetRequests = context.GetTargetRequests();
                    if (targetRequests != null && targetRequests.Count > 0)
                    {
                        var validTargetRequest = new List<Request>();
                        foreach (var targetRequest in targetRequests)
                        {
                            targetRequest.Depth = response.Request.Depth + 1;
                            if (targetRequest.Depth <= Depth)
                            {
                                validTargetRequest.Add(targetRequest);
                            }
                        }

                        var count = _scheduler.Enqueue(validTargetRequest);
                        if (count > 0)
                        {
                            await _statisticsService.IncrementTotalAsync(Id, count);
                        }
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
                    await _statisticsService.PrintStatisticsAsync(Id);
                }
            }
        }

        private Task StartSpeedControllerAsync()
        {
            return Task.Factory.StartNew(async () =>
            {
                _logger.LogInformation($"任务 {Id} 速度控制器启动");
                bool @break = false;


                while (!@break)
                {
                    Thread.Sleep(_speedControllerInterval);

                    switch (_status)
                    {
                        case Status.Running:
                        {
                            try
                            {
                                var requests = _scheduler.Dequeue(Id, _dequeueBatchCount);
                                foreach (var request in requests)
                                {
                                    OnDownloading?.Invoke(request);
                                }

                                if (requests.Length > 0)
                                {
                                    await _downloadService.EnqueueRequests(Id, requests);
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError($"速度控制器运转失败: {e}");
                            }

                            break;
                        }
                        case Status.Paused:
                        {
                            _logger.LogInformation($"任务 {Id} 速度控制器暂停");
                            break;
                        }
                        case Status.Exited:
                        {
                            @break = true;
                            break;
                        }
                    }
                }

                _logger.LogInformation($"任务 {Id} 速度控制器退出");
            });
        }
    }
}