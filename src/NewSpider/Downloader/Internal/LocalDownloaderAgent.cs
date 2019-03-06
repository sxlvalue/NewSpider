using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewSpider.Downloader.Entity;
using NewSpider.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NewSpider.Downloader.Internal
{
    internal class LocalDownloaderAgent : IDownloaderAgent
    {
        private bool _stop;
        private bool _stopped;

        private readonly IMessageQueue _mq;
        private readonly IDownloaderAgentStore _downloaderAgentStore;
        private readonly Guid _id = Guid.NewGuid();
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, DownloaderEntry> _cache =
            new ConcurrentDictionary<string, DownloaderEntry>();

        public LocalDownloaderAgent(IMessageQueue mq, IDownloaderAgentStore downloaderAgentStore)
        {
            _mq = mq;
            _downloaderAgentStore = downloaderAgentStore;
            _logger = Log.CreateLogger<LocalDownloaderAgent>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // 注册节点
            await _downloaderAgentStore.RegisterAsync(
                new DownloaderAgent
                {
                    Id = _id,
                    Name = "Local-downloader"
                });

            // 开始心跳
            StartHeartbeatAsync().ConfigureAwait(false);

            // 订阅节点编号
            _mq.Subscribe(_id.ToString(), async (message) =>
            {
                var commandSeparatorIndex = message.IndexOf('|');
                var command = message.Substring(0, commandSeparatorIndex);
                var json = message.Substring(commandSeparatorIndex + 1);

                switch (command)
                {
                    case "Init":
                    {
                        var options = JsonConvert.DeserializeObject<DownloaderOptions>(json);
                        // TODO：反序列化成参数类
                        await InitDownloaderAsync(options);
                        break;
                    }
                    case "Download":
                    {
                        var requests = JsonConvert.DeserializeObject<List<Request>>(json);
                        if (requests.Count > 0)
                        {
                            //TODO: 以 ownerId 作 dict 并发下载
                            List<Response> responses = new List<Response>();
                            Parallel.ForEach(requests, async (request) =>
                            {
                                var response = await DownloadAsync(request);
                                responses.Add(response);
                            });

                            // TODO: 以 ownerId 作 dict 上传
                            foreach (var response in responses)
                            {
                                await _mq.PublishAsync(
                                    $"{NewSpiderCons.ResponseHandlerTopic}{response.Request.OwnerId}",
                                    JsonConvert.SerializeObject(new[] {response}));
                            }
                        }

                        break;
                    }
                }
            });

            // 循环清理过期下载器
            StartReleaseDownloaderAsync().ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // TODO: UnSubscribe mq
            _stop = true;
            while (!_stopped && !cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(500);
            }

            return Task.CompletedTask;
        }

        private Task StartHeartbeatAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                while (!_stop)
                {
                    Thread.Sleep(1000);

                    _downloaderAgentStore.HeartbeatAsync(new DownloaderAgent
                    {
                        Id = _id,
                        Name = "Local-downloader"
                    });
                    // _logger.LogInformation("Agent heartbeat");
                }

                _stopped = true;
            });
        }

        private async Task<Response> DownloadAsync(Request request)
        {
            var downloader = GetDownloader(request.OwnerId);
            var response = await downloader.DownloadAsync(request);

            _logger.LogInformation($"Download {request.Url} success");
            return response;
        }

        private Task StartReleaseDownloaderAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                while (!_stop)
                {
                    Thread.Sleep(1000);
                    // _logger.LogInformation("Release downloader");
                }
            });
        }

        private IDownloader GetDownloader(string ownerId)
        {
            if (_cache.TryGetValue(ownerId, out var output))
            {
                return output.Downloader;
            }
            else
            {
                throw new NewSpiderException("xx");
            }
        }

        /// <summary>
        /// TODO: 初始化隔离的下载器，可以是 `HttpClient` 也可以是 `浏览器`
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="domain"></param>
        /// <param name="cookie"></param>
        /// <param name="useProxy"></param>
        /// <returns></returns>
        private Task InitDownloaderAsync(DownloaderOptions options)
        {
            if (!_cache.ContainsKey(options.OwnerId))
            {
                _cache.TryAdd(options.OwnerId,
                    new DownloaderEntry
                    {
                        LastUsedTime = DateTime.Now,
                        //TODO: 
                        Downloader = new EmptyDownloader()
                    });
            }

            return Task.CompletedTask;
        }

        class DownloaderEntry
        {
            public IDownloader Downloader { get; set; }
            public DateTime LastUsedTime { get; set; }
        }
    }
}