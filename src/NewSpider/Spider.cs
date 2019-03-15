using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewSpider.Data;
using NewSpider.Data.Pipeline;
using NewSpider.Data.Processor;
using NewSpider.Downloader;
using NewSpider.Downloader.Entity;
using NewSpider.Downloader.Internal;
using NewSpider.Infrastructure;
using NewSpider.MessageQueue;
using NewSpider.Scheduler;
using Newtonsoft.Json;

namespace NewSpider
{
    public partial class Spider : ISpider
    {
        private readonly IList<IPageProcessor> _processors = new List<IPageProcessor>();
        private readonly IList<IPipeline> _pipelines = new List<IPipeline>();
        private readonly IList<IDataFlow> _dataFlows = new List<IDataFlow>();
        private readonly IList<Request> _requests = new List<Request>();

        private readonly IMessageQueue _mq;
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;
        private readonly IDownloadService _downloadService;
        private readonly IStatisticsService _statisticsService;
        private DateTime _lastRequestTime;
        private event RequestHandler BeforeDownload;
        private Semaphore _semaphore;
        private Status _status;
        private uint _emptySleepTime = 30;

        public uint Speed { get; set; } = 1;

        public uint RequestBatchCount { get; set; } = 1000;

        public uint StatisticsInterval { get; set; } = 5;

        public uint DownloaderCount { get; set; } = 1;

        public string Id { get; set; }
        public string Name { get; set; }

        public int RetryDownloadTimes { get; set; }

        public uint EmptySleepTime
        {
            get => _emptySleepTime;
            set
            {
                if (value < 30)
                {
                    throw new NewSpiderException("EmptySleepTime should larger than 30");
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

            _logger = loggerFactory.CreateLogger(typeof(Spider).Name);
        }

        public async Task RunAsync()
        {
            try
            {
                _status = Status.Running;
                await _statisticsService.StartAsync(Id);
                if (_requests.Count > 0)
                {
                    await _scheduler.PushAsync(Id, _requests);
                    await _statisticsService.TotalAsync(Id, (uint) _requests.Count);
                    _logger.LogInformation($"任务 {Id} 请求推送到调度器: {_requests.Count}");
                    _requests.Clear();
                }

                // 分配下载器: 可以通过消息队列(本地模式)或者HTTP接口(配合Portal)分配
                var allocated = await _downloadService.AllocateAsync(new AllotDownloaderMessage
                {
                    OwnerId = Id,
                    Type = DownloaderType.Sample,
                    Speed = Speed,
                    UseProxy = false
                });
                if (!allocated)
                {
                    return;
                }

                _logger.LogInformation($"任务 {Id} 分配下载器成功");

                // 启动速度控制器
                StartSpeedControllerAsync().ConfigureAwait(false);

                // 合并数据处理流程
                var dataFlows = GetDataFlows();

                _mq.Subscribe($"{NewSpiderConsts.ResponseHandlerTopic}{Id}", async (message) =>
                {
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        _logger.LogWarning($"任务 {Id} 接收到空消息");
                        return;
                    }

                    _lastRequestTime = DateTime.Now;
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
                        await _statisticsService.DownloadSuccessAsync(agentId, successResponses.Count,
                            elapsedMilliseconds);
                    }

                    Parallel.ForEach(successResponses, async (response) =>
                    {
                        _logger.LogInformation($"任务 {Id} 下载 {response.Request.Url} 成功");

                        var context = new DataFlowContext();
                        context.AddRequest(response.Request);
                        try
                        {
                            bool success = false;
                            foreach (var dataFlow in dataFlows)
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
                                await _scheduler.PushAsync(Id, context.Properties["ExtractedRequests"]);
                            }

                            if (success)
                            {
                                await _statisticsService.SuccessAsync(Id);
                            }
                            else
                            {
                                await _statisticsService.FailedAsync(Id);
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
                        await _statisticsService.DownloadFailedAsync(agentId, failedRequests.Count);
                    }

                    await _scheduler.PushAsync(Id, retryResponses.Select(x => x.Request));
                });

                _lastRequestTime = DateTime.Now;
                int waited = 0;
                while (_semaphore != Semaphore.Exit)
                {
                    if ((DateTime.Now - _lastRequestTime).Seconds > EmptySleepTime)
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

                _logger.LogInformation($"任务 {Id} 退出");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            finally
            {
                _status = Status.Exited;
                await _statisticsService.ExitAsync(Id);
                await PrintStatistics();
            }
        }

        public void Pause()
        {
            _semaphore = Semaphore.Pause;
        }

        public void Continue()
        {
            _semaphore = Semaphore.Run;
        }

        public void Exit()
        {
            _semaphore = Semaphore.Exit;
        }

        private List<IDataFlow> GetDataFlows()
        {
            var dataFlows = new List<IDataFlow>();
            dataFlows.AddRange(_processors);
            dataFlows.AddRange(_dataFlows);
            dataFlows.AddRange(_pipelines);
            return dataFlows;
        }

        private async Task PrintStatistics()
        {
            var statistics = await _statisticsService.GetSpiderStatisticsAsync(Id);
            _logger.LogTrace(
                $"任务 {Id} 总计 {statistics.Total}, 成功 {statistics.Success}, 失败 {statistics.Failed}, 剩余 {(statistics.Total - statistics.Success - statistics.Failed)}");
        }
    }
}