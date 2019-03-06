using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewSpider.Downloader.Entity;
using NewSpider.Infrastructure;
using Newtonsoft.Json;

namespace NewSpider.Downloader.Internal
{
    internal class LocalDownloaderManager : IDownloaderManager
    {
        private readonly IMessageQueue _mq;
        private readonly IDownloaderAgentStore _downloaderAgentStore;
        private readonly ILogger _logger;

        public IDownloaderAgentStore Store => _downloaderAgentStore;

        public LocalDownloaderManager(IMessageQueue mq, IDownloaderAgentStore downloaderAgentStore)
        {
            _mq = mq;
            _downloaderAgentStore = downloaderAgentStore;
            _logger = Log.CreateLogger(typeof(LocalDownloaderManager).Name);
        }

        public async Task RegisterAsync(DownloaderOptions options)
        {
            var agents = (await _downloaderAgentStore.GetAvailableAsync()).ToArray();
            if (agents.Length <= 0)
            {
                _logger.LogError("No available downloader agent");
            }

            var json = JsonConvert.SerializeObject(options);
            foreach (var agent in agents)
            {
                await _mq.PublishAsync(agent.Id.ToString(), $"Init|{json}");
            }
        }

        public async Task PublishAsync(IEnumerable<IRequest> requests)
        {
            // 1. 取所有可用 agent
            var agents = (await _downloaderAgentStore.GetAvailableAsync()).ToArray();
            if (agents.Length <= 0)
            {
                _logger.LogError("No available downloader agent");
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
        }
    }
}