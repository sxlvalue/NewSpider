using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewSpider.Downloader
{
    public class LocalDownloaderService : IDownloaderService
    {      
        private readonly IMessageQueue _mq;
        private readonly IDownloaderAgentStore _downloaderAgentStore;
        

        public LocalDownloaderService(IMessageQueue mq,IDownloaderAgentStore downloaderAgentStore)
        {
            _mq = mq;
            _downloaderAgentStore = downloaderAgentStore;
        }
        
        public Task RegisterAsync(string ownerId, int nodeCount, int threadNum, string domain = null, string cookie = null,
            bool useProxy = false, bool inherit = false)
        {
            return Task.CompletedTask;
        }

        public Task PublishAsync(string ownerId, IEnumerable<IRequest> requests)
        {
            // 1. 取所有可用 agent
            
            // 2. 按照策略发送给指定 aget 消息
            _mq.PublishAsync("agentId", "");
            throw new System.NotImplementedException();
        }

        public Task ShutDownDownloader(string downloaderId)
        {
            throw new System.NotImplementedException();
        }

        public Task ExcludeDownloader(string ownerId, string downloaderId)
        {
            throw new System.NotImplementedException();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}