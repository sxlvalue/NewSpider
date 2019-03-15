using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewSpider.Data;
using NewSpider.Downloader.Entity;
using NewSpider.Infrastructure;
using NewSpider.MessageQueue;
using Newtonsoft.Json;

namespace NewSpider.Downloader
{
    internal abstract class AbstractDownloaderAgent : IDownloaderAgent
    {
        private bool _isRunning;

        private readonly IMessageQueue _mq;
        private readonly string _agentId;
        private readonly string _name;

        private readonly ConcurrentDictionary<string, DownloaderEntry> _cache =
            new ConcurrentDictionary<string, DownloaderEntry>();

        protected ILogger Logger { get; }

        protected AbstractDownloaderAgent(string agentId, string name, IMessageQueue mq, ILoggerFactory loggerFactory)
        {
            Check.NotNull(agentId, nameof(agentId));
            Check.NotNull(name, nameof(name));
            _agentId = agentId;
            _name = name;
            _mq = mq;
            Logger = loggerFactory.CreateLogger(GetType().FullName);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
            {
                throw new NewSpiderException("下载器代理正在运行中");
            }

            Logger.LogInformation("本地下载代理启动");

            _isRunning = true;

            // 注册节点
            var json = JsonConvert.SerializeObject(new DownloaderAgent
            {
                Id = _agentId,
                Name = _name
            });
            await _mq.PublishAsync(NewSpiderConsts.DownloaderCenterTopic, $"{NewSpiderConsts.RegisterCommand}|{json}");

            // 开始心跳
            HeartbeatAsync(cancellationToken).ConfigureAwait(false);

            // 订阅节点编号
            _mq.Subscribe(_agentId, async message =>
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    Logger.LogWarning("接收到空消息");
                    return;
                }

                try
                {
                    var commandMessage = message.ToCommandMessage();

                    if (commandMessage == null)
                    {
                        Logger.LogWarning($"接收到非法消息: {message}");
                        return;
                    }

                    switch (commandMessage.Command)
                    {
                        case NewSpiderConsts.AllocateDownloaderCommand:
                        {
                            await AllotDownloaderAsync(commandMessage.Message);
                            break;
                        }
                        case NewSpiderConsts.DownloadCommand:
                        {
                            await DownloadAsync(commandMessage.Message).ConfigureAwait(false);
                            break;
                        }
                        default:
                        {
                            Logger.LogError($"无法处理消息 {message}");
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError($"处理消息 {message} 失败: {e}");
                }
            });

            // 循环清理过期下载器
            ReleaseDownloaderAsync().ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _mq.Unsubscribe(_agentId);
            _isRunning = false;
            Logger.LogInformation("本地下载代理退出");
            return Task.CompletedTask;
        }

        protected virtual Task<DownloaderEntry> CreateDownloaderEntry(AllotDownloaderMessage allotDownloaderMessage)
        {
            DownloaderEntry downloaderEntry = null;
            // TODO: 添加其它下载器的分配方法
            switch (allotDownloaderMessage.Type)
            {
                case DownloaderType.Empty:
                {
                    downloaderEntry = new DownloaderEntry
                    {
                        LastUsedTime = DateTime.Now,
                        Downloader = new EmptyDownloader
                        {
                            Logger = Logger,
                            AgentId = _agentId
                        }
                    };
                    break;
                }
                case DownloaderType.Sample:
                {
                    downloaderEntry = new DownloaderEntry
                    {
                        LastUsedTime = DateTime.Now,
                        Downloader = new SampleDownloader
                        {
                            Logger = Logger,
                            AgentId = _agentId
                        }
                    };
                    break;
                }
                case DownloaderType.WebDriver:
                {
                    throw new NotImplementedException();
                }
                case DownloaderType.HttpClient:
                {
                    throw new NotImplementedException();
                }
            }

            return Task.FromResult(downloaderEntry);
        }

        private Task HeartbeatAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(async () =>
            {
                while (_isRunning)
                {
                    Thread.Sleep(5000);

                    var json = JsonConvert.SerializeObject(new DownloaderAgentHeartbeat
                    {
                        Id = _agentId,
                        Name = _name
                    });
                    await _mq.PublishAsync(NewSpiderConsts.DownloaderCenterTopic,
                        $"{NewSpiderConsts.HeartbeatCommand}|{json}");
                }
            }, cancellationToken);
        }


        private Task DownloadAsync(string message)
        {
            return Task.Factory.StartNew(() =>
            {
                var requests = JsonConvert.DeserializeObject<Request[]>(message);
                if (requests.Length > 0)
                {
                    // 下载中心可能把下载请求批量传送，因此反序列化的请求需要按拥有者标号分组。
                    // 对于同一个拥有者应该是顺序下载。因为并发控制已经由下载中心控制好了
                    var groupings = requests.GroupBy(x => x.OwnerId).ToDictionary(x => x.Key, y => y.ToList());
                    foreach (var grouping in groupings)
                    {
                        foreach (var request in grouping.Value)
                        {
                            Task.Factory.StartNew(async () =>
                            {
                                var response = await DownloadAsync(request);
                                if (response != null)
                                {
                                    if (!response.Success)
                                    {
                                    }

                                    await _mq.PublishAsync($"{NewSpiderConsts.ResponseHandlerTopic}{grouping.Key}",
                                        JsonConvert.SerializeObject(new[] {response}));
                                }
                            }).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    Logger.LogWarning("没有找到需要下载的请求");
                }
            });
        }

        private async Task<Response> DownloadAsync(Request request)
        {
            if (_cache.TryGetValue(request.OwnerId, out DownloaderEntry downloaderEntry))
            {
                var response = await downloaderEntry.Downloader.DownloadAsync(request);
                downloaderEntry.LastUsedTime = DateTime.Now;
                return response;
            }
            else
            {
                Logger.LogError($"找不到 {request.OwnerId} 的下载器");
                return null;
            }
        }

        private Task ReleaseDownloaderAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                while (_isRunning)
                {
                    Thread.Sleep(1000);

                    var now = DateTime.Now;
                    var expiredDownloaderEntries = new List<string>();
                    foreach (var kv in _cache)
                    {
                        var downloaderEntry = kv.Value;
                        if ((now - downloaderEntry.LastUsedTime).TotalSeconds > 300)
                        {
                            expiredDownloaderEntries.Add(kv.Key);
                        }
                    }

                    foreach (var expiredDownloaderEntry in expiredDownloaderEntries)
                    {
                        _cache.TryRemove(expiredDownloaderEntry, out _);
                    }

                    Logger.LogDebug($"释放过期下载器: {expiredDownloaderEntries.Count}");
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task AllotDownloaderAsync(string message)
        {
            var allotDownloaderMessage = JsonConvert.DeserializeObject<AllotDownloaderMessage>(message);
            if (!_cache.ContainsKey(allotDownloaderMessage.OwnerId))
            {
                var downloaderEntry = await CreateDownloaderEntry(allotDownloaderMessage);
                if (downloaderEntry == null)
                {
                    Logger.LogError($"任务 {allotDownloaderMessage.OwnerId} 分配下载器 {allotDownloaderMessage.Type} 失败");
                }
                else
                {
                    _cache.TryAdd(allotDownloaderMessage.OwnerId, downloaderEntry);
                }
            }
            else
            {
                Logger.LogWarning($"任务 {allotDownloaderMessage.OwnerId} 重复分配下载器");
            }
        }
    }

    class DownloaderEntry
    {
        public IDownloader Downloader { get; set; }
        public DateTime LastUsedTime { get; set; }
    }
}