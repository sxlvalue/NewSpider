using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetSpider.Core;
using DotnetSpider.Downloader.Entity;
using DotnetSpider.Downloader.Internal;
using DotnetSpider.MessageQueue;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DotnetSpider.Downloader
{
    public abstract class AbstractDownloadCenter : IDownloadCenter
    {
        private bool _isRunning;

        protected readonly IMessageQueue Mq;
        protected readonly ILogger Logger;
        protected readonly IDownloaderAgentStore DownloaderAgentStore;

        public AbstractDownloadCenter(IMessageQueue mq, IDownloaderAgentStore downloaderAgentStore,
            ILoggerFactory loggerFactory)
        {
            Mq = mq;
            DownloaderAgentStore = downloaderAgentStore;
            Logger = loggerFactory.CreateLogger<LocalDownloadCenter>();
        }

        public abstract Task<bool> AllocateAsync(AllotDownloaderMessage allotDownloaderMessage);

        /// <summary>
        /// TODO: 根据策略分配下载器: 1. Request 从哪个下载器返回的需要返回到对应的下载器  2. 随机一个下载器
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="requests"></param>
        /// <returns></returns>
        public abstract Task EnqueueRequests(string ownerId, IEnumerable<Request> requests);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
            {
                throw new SpiderException("下载中心正在运行中");
            }

            Logger.LogInformation("本地下载中心启动");

            Mq.Subscribe(Framework.DownloaderCenterTopic, async message =>
            {
                var commandMessage = message.ToCommandMessage();
                if (commandMessage == null)
                {
                    Logger.LogWarning($"接收到非法消息: {message}");
                    return;
                }

                switch (commandMessage.Command)
                {
                    case "Register":
                    {
                        var agent = JsonConvert.DeserializeObject<DownloaderAgent>(commandMessage.Message);
                        Logger.LogInformation($"注册下载器: {agent.Id}");
                        await DownloaderAgentStore.RegisterAsync(agent);
                        break;
                    }
                    case "Heartbeat":
                    {
                        var heartbeat = JsonConvert.DeserializeObject<DownloaderAgentHeartbeat>(commandMessage.Message);
                        await DownloaderAgentStore.HeartbeatAsync(heartbeat);
                        break;
                    }
                    case "Allocate":
                    {
                        var options = JsonConvert.DeserializeObject<AllotDownloaderMessage>(commandMessage.Message);
                        await AllocateAsync(options);
                        break;
                    }
                    case "Download":
                    {
                        var requests = JsonConvert.DeserializeObject<Request[]>(commandMessage.Message);
                        if (requests.Length > 0)
                        {
                            var ownerId = requests.First().OwnerId;
                            await EnqueueRequests(ownerId, requests);
                        }

                        break;
                    }
                }
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Mq.Unsubscribe(Framework.DownloaderCenterTopic);
            _isRunning = false;
            Logger.LogInformation("本地下载中心退出");
            return Task.CompletedTask;
        }
    }
}