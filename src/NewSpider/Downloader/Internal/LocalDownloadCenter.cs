using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewSpider.Downloader.Entity;
using NewSpider.Infrastructure;
using NewSpider.MessageQueue;
using Newtonsoft.Json;

namespace NewSpider.Downloader.Internal
{
    public class LocalDownloadCenter : IDownloadCenter
    {
        private bool _isRunning;

        private readonly IMessageQueue _mq;
        private readonly ILogger _logger;
        private readonly IDownloaderAgentStore _downloaderAgentStore;

        public LocalDownloadCenter(IMessageQueue mq, IDownloaderAgentStore downloaderAgentStore,
            ILogger<LocalDownloadCenter> logger)
        {
            _mq = mq;
            _downloaderAgentStore = downloaderAgentStore;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
            {
                throw new NewSpiderException("下载中心正在运行中");
            }

            _mq.Subscribe(NewSpiderConsts.DownloaderCenterTopic, async (message) =>
            {
                var commandMessage = message.ToCommandMessage();
                if (commandMessage == null)
                {
                    _logger.LogWarning($"接收到非法消息: {message}");
                    return;
                }


                switch (commandMessage.Command)
                {
                    case "Register":
                    {
                        var heartbeat = JsonConvert.DeserializeObject<DownloaderAgentHeartbeat>(commandMessage.Message);
                        await _downloaderAgentStore.RegisterAsync(heartbeat);
                        break;
                    }
                    case "Heartbeat":
                    {
                        var heartbeat = JsonConvert.DeserializeObject<DownloaderAgentHeartbeat>(commandMessage.Message);
                        await _downloaderAgentStore.HeartbeatAsync(heartbeat);
                        break;
                    }
                    case "Allocate":
                    {
                        var options = JsonConvert.DeserializeObject<AllotDownloaderMessage>(message);
                        // TODO: 根据策略分配下载器
                        var agents = (await _downloaderAgentStore.GetAvailableAsync()).ToArray();
                        if (agents.Length <= 0)
                        {
                            _logger.LogError("未找到活跃的下载器代理");
                        }

                        foreach (var agent in agents)
                        {
                            await _mq.PublishAsync(agent.Id, message);
                        }
                        // 保存节点选取信息
                        await _downloaderAgentStore.AllocateAsync(options.OwnerId, agents.Select(x => x.Id));
                        break;
                    }
                    case "Download":
                    {
                        // TODO: 根据策略分配下载器: 1. Request 从哪个下载器返回的需要返回到对应的下载器  2. 随机一个下载器
                        // 1. 取所有可用 agent
                        var agents = (await _downloaderAgentStore.GetAvailableAsync()).ToArray();
                        if (agents.Length <= 0)
                        {
                            _logger.LogError("未找到活跃的下载器代理");
                        }

                        var agentIndex = 0;
                        foreach (var request in requests)
                        {
                            var agent = agents[agentIndex];
                            agentIndex++;
                            if (agentIndex >= agents.Length)
                            {
                                agentIndex = 0;
                            }

                            var json = JsonConvert.SerializeObject(new[] {request});
                            await _mq.PublishAsync(agent.Id.ToString(), $"Download|{json}");
                        }

                        break;
                    }
                }
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _mq.Unsubscribe(NewSpiderConsts.DownloaderCenterTopic);
            _isRunning = false;
            return Task.CompletedTask;
        }
    }
}