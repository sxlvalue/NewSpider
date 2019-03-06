using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NewSpider.Downloader.Entity;
using NewSpider.Infrastructure;

namespace NewSpider.Downloader.Internal
{
    internal class LocalDownloaderAgentStore : IDownloaderAgentStore
    {
        private static volatile ConcurrentDictionary<Guid, DownloaderAgent> _agents =
            new ConcurrentDictionary<Guid, DownloaderAgent>();

        public Task<IEnumerable<DownloaderAgent>> GetAvailableAsync()
        {
            return Task.FromResult(_agents.Values as IEnumerable<DownloaderAgent>);
        }

        public Task RegisterAsync(DownloaderAgent agent)
        {
            agent.LastModifcationTime = DateTime.Now;
            _agents.TryAdd(agent.Id, agent);
            return Task.CompletedTask;
        }

        public Task HeartbeatAsync(DownloaderAgent agent)
        {
            agent.LastModifcationTime = DateTime.Now;
            _agents[agent.Id] = agent;
            return Task.CompletedTask;
        }
    }
}