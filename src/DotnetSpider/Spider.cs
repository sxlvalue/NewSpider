using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotnetSpider.Core;
using DotnetSpider.Data;
using DotnetSpider.Data.Storage;
using DotnetSpider.Downloader;
using DotnetSpider.Downloader.Entity;
using DotnetSpider.MessageQueue;
using DotnetSpider.RequestSupply;
using DotnetSpider.Scheduler;
using DotnetSpider.Statistics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("DotnetSpider.Tests")]

namespace DotnetSpider
{
    public partial class Spider
    {
        private readonly IServiceProvider _services;

        protected virtual void Initialize()
        {
        }

        protected void NewId()
        {
            Id = Guid.NewGuid().ToString("N");
        }

        public Spider(IServiceProvider services)
        {
            _services = services;
            _downloadService = services.GetRequiredService<IDownloadService>();
            _statisticsService = services.GetRequiredService<IStatisticsService>();
            _mq = services.GetRequiredService<IMessageQueue>();
            _loggerFactory = services.GetRequiredService<ILoggerFactory>();
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

        public Spider AddRequestSupply(IRequestSupply supply)
        {
            CheckIfRunning();
            _requestSupplies.Add(supply);
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
            return Task.Factory.StartNew(async () =>
            {
                try
                {
                    // 初始化设置
                    Initialize();

                    _scheduler = _scheduler ?? new QueueDistinctBfsScheduler();

                    Status = Status.Running;

                    // 添加任务启动的监控信息
                    await _statisticsService.StartAsync(Id);

                    // 通过供应接口添加请求
                    foreach (var requestSupply in _requestSupplies)
                    {
                        requestSupply.Run(request => AddRequests(request));
                    }

                    // 把列表中可能剩余的请求加入队列
                    EnqueueRequests();

                    // 分配下载器: 可以通过消息队列(本地模式)或者HTTP接口(配合Portal)分配
                    var allocated = await AllotDownloaderAsync();
                    if (!allocated)
                    {
                        _logger.LogError($"任务 {Id} 分配下载器失败");
                        return;
                    }

                    _logger.LogInformation($"任务 {Id} 分配下载器成功");

                    // 初始化各数据流处理器
                    foreach (var dataFlow in _dataFlows)
                    {
                        await dataFlow.InitAsync();
                    }

                    // 订阅数据流
                    _mq.Subscribe($"{Framework.ResponseHandlerTopic}{Id}",
                        async message => await HandleMessage(message));

                    // 启动速度控制器
                    StartSpeedControllerAsync().ConfigureAwait(false).GetAwaiter();

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
                    Status = Status.Exited;
                    _logger.LogInformation($"任务 {Id} 退出");
                }
            });
        }

        public void Pause()
        {
            Status = Status.Paused;
        }

        public void Continue()
        {
            Status = Status.Running;
        }

        public void Exit()
        {
            _logger.LogInformation("退出中...");
            Status = Status.Exiting;
            // 直接取消订阅即可: 1. 如果是本地应用, 
            _mq.Unsubscribe($"{Framework.ResponseHandlerTopic}{Id}");
        }

        protected StorageBase GetDefaultStorage()
        {
            var options = _services.GetRequiredService<SpiderOptions>();
            switch (options.Storage)
            {
                case "MySql":
                {
                    return new MySqlEntityStorage(options.StorageType, options.ConnectionString);
                }
                case "Mongo":
                {
                    return new MongoEntityStorage(options.ConnectionString);
                }
                case "Postgre":
                {
                    return new PostgreSqlEntityStorage(options.StorageType, options.ConnectionString);
                }
                default:
                {
                    throw new SpiderException("未能从配置文件解析出正确的存储器");
                }
            }
        }

        private async Task<bool> AllotDownloaderAsync()
        {
            return await _downloadService.AllocateAsync(new AllotDownloaderMessage
            {
                OwnerId = Id,
                AllowAutoRedirect = DownloaderOptions.AllowAutoRedirect,
                UseProxy = DownloaderOptions.UseProxy,
                DownloaderCount = DownloaderOptions.DownloaderCount,
                Cookies = DownloaderOptions.Cookies,
                DecodeHtml = DownloaderOptions.DecodeHtml,
                Timeout = DownloaderOptions.Timeout,
                Type = DownloaderOptions.Type,
                UseCookies = DownloaderOptions.UseCookies
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
            Parallel.ForEach(successResponses, async response =>
            {
                _logger.LogInformation($"任务 {Id} 下载 {response.Request.Url} 成功");

                var context = new DataFlowContext(_services.CreateScope().ServiceProvider);
                context.AddResponse(response);
                try
                {
                    bool success = true;
                    foreach (var dataFlow in _dataFlows)
                    {
                        var dataFlowResult = await dataFlow.HandleAsync(context);
                        switch (dataFlowResult)
                        {
                            case DataFlowResult.Success:
                            {
                                continue;
                            }
                            case DataFlowResult.Failed:
                            {
                                _logger.LogError($"任务 {Id} 数据流处理器 {dataFlow.GetType().Name} 失败");
                                success = false;
                                break;
                            }
                            case DataFlowResult.Terminated:
                            {
                                break;
                            }
                        }
                    }


                    var resultItems = context.GetItems();
                    // 如果解析结果为空，重试
                    if ((resultItems == null || resultItems.Sum(x => x.Value == null ? 0 : x.Value.Count) == 0) &&
                        RetryWhenResultIsEmpty)
                    {
                        response.Request.RetriedTimes++;
                        response.Request.ComputeHash();
                        // 不需要添加总计
                        _scheduler.Enqueue(new[] {response.Request.Clone()});
                    }

                    // 解析的目标请求
                    var followRequests = context.GetTargetRequests();
                    if (followRequests != null && followRequests.Count > 0)
                    {
                        var requests = new List<Request>();
                        foreach (var followRequest in followRequests)
                        {
                            followRequest.Depth = response.Request.Depth + 1;
                            if (followRequest.Depth <= Depth)
                            {
                                requests.Add(followRequest);
                            }
                        }

                        var count = _scheduler.Enqueue(requests);
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
                responses.Where(x => !x.Success)
                    .ToList();
            // 统计下载失败
            if (failedRequests.Count > 0)
            {
                await _statisticsService.IncrementFailedAsync(Id);
                await _statisticsService.IncrementDownloadFailedAsync(agentId, failedRequests.Count);
            }

            var retryCount = _scheduler.Enqueue(retryResponses.Select(x => x.Request.Clone()));
            if (retryCount > 0)
            {
                await _statisticsService.IncrementTotalAsync(Id, retryCount);
            }
        }

        /// <summary>
        /// 发送退出信号
        /// </summary>
        internal void ExitBySignal()
        {
            if (MmfSignal)
            {
                var mmf = MemoryMappedFile.CreateFromFile(Id, FileMode.OpenOrCreate, null, 4,
                    MemoryMappedFileAccess.ReadWrite);
                using (var accessor = mmf.CreateViewAccessor())
                {
                    accessor.Write(0, true);
                    accessor.Flush();
                }
            }
            else
            {
                throw new SpiderException("未开启 MMF 控制");
            }
        }

        internal async Task WaitForExit()
        {
            int waited = 0;
            while (!(Status == Status.Exited || Status == Status.Exiting))
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


                MemoryMappedFile mmf = MmfSignal
                    ? MemoryMappedFile.CreateFromFile(Id, FileMode.OpenOrCreate, null, 4,
                        MemoryMappedFileAccess.ReadWrite)
                    : null;

                using (var accessor = mmf?.CreateViewAccessor())
                {
                    accessor?.Write(0, false);
                    accessor?.Flush();

                    while (!@break)
                    {
                        Thread.Sleep(_speedControllerInterval);

                        switch (Status)
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

                        if (accessor != null && accessor.ReadBoolean(0))
                        {
                            Exit();
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
            if (Status == Status.Running)
            {
                throw new SpiderException("爬虫正在运行");
            }
        }

        private void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Exit();
            while (Status != Status.Exited)
            {
                Thread.Sleep(500);
            }
        }

        private void EnqueueRequests()
        {
            if (_requests.Count <= 0) return;

            _scheduler = _scheduler ?? new QueueDistinctBfsScheduler();
            var count = _scheduler.Enqueue(_requests);
            _statisticsService.IncrementTotalAsync(Id, count).ConfigureAwait(false);
            _logger.LogInformation($"任务 {Id} 请求推送到调度器: {_requests.Count}");
            _requests.Clear();
        }
    }
}