using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public partial class Spider
    {
        private readonly SpiderOptions _options;

        protected virtual void Initialize()
        {
        }

        public Spider(IMessageQueue mq,
            IDownloadService downloadService,
            IStatisticsService statisticsService,
            SpiderOptions options,
            ILoggerFactory loggerFactory)
        {
            _downloadService = downloadService;
            _statisticsService = statisticsService;
            _mq = mq;
            _loggerFactory = loggerFactory;
            _options = options;
            _logger = _loggerFactory.CreateLogger(typeof(Spider).Name);

            Console.CancelKeyPress += ConsoleCancelKeyPress;
        }

        public Spider AddDataFlow(IDataFlow dataFlow)
        {
            CheckIfRunning();
            dataFlow.Logger = _loggerFactory.CreateLogger(dataFlow.GetType());
            _dataFlows.Add(dataFlow);
            return this;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Spider AddRequests(params Request[] requests)
        {
            foreach (var request in requests)
            {
                request.OwnerId = Id;
                request.Depth = 1;
                _requests.Add(request);
                if (_requests.Count % EnqueueBatchCount == 0)
                {
                    EnqueueRequests();
                }
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Spider AddRequests(params string[] urls)
        {
            foreach (var url in urls)
            {
                var request = new Request {Url = url, OwnerId = Id, Depth = 1};
                _requests.Add(request);
                if (_requests.Count % EnqueueBatchCount == 0)
                {
                    EnqueueRequests();
                }
            }

            return this;
        }

        public Task RunAsync(params string[] args)
        {
            CheckIfRunning();
            Initialize();
            _scheduler = _scheduler ?? new QueueDistinctBfsScheduler();
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    _status = Status.Running;
                    // 添加任务启动的监控信息
                    await _statisticsService.StartAsync(Id);

                    EnqueueRequests();

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
                    _mq.Subscribe($"{Framework.ResponseHandlerTopic}{Id}",
                        async message => await HandleMessage(message));

                    _lastRequestedTime = DateTime.Now;

                    await WaitForExit();
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString());
                }
                finally
                {
                    foreach (var dataFlow in _dataFlows)
                    {
                        try
                        {
                            dataFlow.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"释放 {dataFlow.GetType().Name} 失败: {ex}");
                        }
                    }

                    // 添加任务退出的监控信息
                    await _statisticsService.ExitAsync(Id);

                    await _statisticsService.PrintStatisticsAsync(Id);
                    _status = Status.Exited;
                    _logger.LogInformation($"任务 {Id} 退出");
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
            _logger.LogInformation("退出中...");
            _status = Status.Exiting;
            // 直接取消订阅即可: 1. 如果是本地应用, 
            _mq.Unsubscribe($"{Framework.ResponseHandlerTopic}{Id}");
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

                var context = new DataFlowContext
                {
                    Options = _options
                };
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
            while (!(_status == Status.Exited || _status == Status.Exiting))
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
                        case Status.Exiting:
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

        /// <summary>
        /// Check whether spider is running.
        /// </summary>
        private void CheckIfRunning()
        {
            if (_status == Status.Running)
            {
                throw new SpiderException("爬虫正在运行");
            }
        }

        private void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Exit();
            while (_status != Status.Exited)
            {
                Thread.Sleep(500);
            }
        }

        private void EnqueueRequests()
        {
            if (_requests.Count <= 0) return;

            var count = _scheduler.Enqueue(_requests);
            _statisticsService.IncrementTotalAsync(Id, count).ConfigureAwait(false);
            _logger.LogInformation($"任务 {Id} 请求推送到调度器: {_requests.Count}");
            _requests.Clear();
        }
    }
}