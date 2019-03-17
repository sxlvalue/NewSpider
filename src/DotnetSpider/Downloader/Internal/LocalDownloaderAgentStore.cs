using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetSpider.Downloader.Entity;

namespace DotnetSpider.Downloader.Internal
{
    internal class LocalDownloaderAgentStore : IDownloaderAgentStore
    {
        private static volatile ConcurrentDictionary<string, DownloaderAgent> _agents =
            new ConcurrentDictionary<string, DownloaderAgent>();

        private static volatile ConcurrentDictionary<string, IEnumerable<string>> _allocatedAgents =
            new ConcurrentDictionary<string, IEnumerable<string>>();

        public Task<List<DownloaderAgent>> GetAllListAsync()
        {
            return Task.FromResult(_agents.Values.ToList());
        }

        public Task<List<DownloaderAgent>> GetAllListAsync(string ownerId)
        {
            if (_allocatedAgents.ContainsKey(ownerId))
            {
                var agentIds = _allocatedAgents[ownerId].ToList();

                var agents = _agents.Where(x => agentIds.Contains(x.Key)).Select(x => x.Value).ToList();
                return Task.FromResult(agents);
            }
            else
            {
                return null;
            }
        }

        public Task RegisterAsync(DownloaderAgent agent)
        {
            agent.LastModificationTime = DateTime.Now;
            _agents.TryAdd(agent.Id, agent);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 本地代理不需要留存心跳
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        public Task HeartbeatAsync(DownloaderAgentHeartbeat agent)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="agentIds"></param>
        /// <returns></returns>
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