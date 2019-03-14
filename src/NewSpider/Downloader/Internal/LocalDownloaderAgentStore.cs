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
        private static volatile ConcurrentDictionary<string, DownloaderAgentHeartbeat> _agents =
            new ConcurrentDictionary<string, DownloaderAgentHeartbeat>();
        private static volatile  ConcurrentDictionary<string, IEnumerable<string>> _allocatedAgents =
            new ConcurrentDictionary<string, IEnumerable<string>>();

        public Task<IEnumerable<DownloaderAgentHeartbeat>> GetAvailableAsync()
        {
            return Task.FromResult(_agents.Values as IEnumerable<DownloaderAgentHeartbeat>);
        }

        public Task RegisterAsync(DownloaderAgentHeartbeat agent)
        {
            agent.LastModificationTime = DateTime.Now;
            _agents.TryAdd(agent.Id, agent);
            return Task.CompletedTask;
        }

        public Task HeartbeatAsync(DownloaderAgentHeartbeat agent)
        {
            agent.LastModificationTime = DateTime.Now;
            _agents[agent.Id] = agent;
            return Task.CompletedTask;
        }

        public Task AllocateAsync(string ownerId, IEnumerable<string> agentIds)
        {
            if (_allocatedAgents.ContainsKey(ownerId))
            {
                _allocatedAgents[ownerId] = agentIds;
            }
            else
            {
                _allocatedAgents.TryAdd(ownerId, agentIds);
            }
            return Task.CompletedTask;
        }
    }
}