using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotnetSpider.Statistics.Entity;

namespace DotnetSpider.Statistics
{
    public class MemoryStatisticsStore : IStatisticsStore
    {
        private readonly ConcurrentDictionary<string, SpiderStatistics> _spiderStatisticsDict =
            new ConcurrentDictionary<string, SpiderStatistics>();

        private readonly ConcurrentDictionary<string, DownloadStatistics> _downloadStatisticsDict =
            new ConcurrentDictionary<string, DownloadStatistics>();

        public Task IncrementTotalAsync(string ownerId, int count)
        {
            if (!_spiderStatisticsDict.ContainsKey(ownerId))
            {
                _spiderStatisticsDict.TryAdd(ownerId, new SpiderStatistics());
            }

            _spiderStatisticsDict[ownerId].AddTotal(count);
            return Task.CompletedTask;
        }

        public Task IncrementSuccessAsync(string ownerId)
        {
            if (!_spiderStatisticsDict.ContainsKey(ownerId))
            {
                _spiderStatisticsDict.TryAdd(ownerId, new SpiderStatistics());
            }

            _spiderStatisticsDict[ownerId].IncSuccess();
            return Task.CompletedTask;
        }

        public Task IncrementFailedAsync(string ownerId, int count = 1)
        {
            if (!_spiderStatisticsDict.ContainsKey(ownerId))
            {
                _spiderStatisticsDict.TryAdd(ownerId, new SpiderStatistics());
            }

            _spiderStatisticsDict[ownerId].AddFailed(count);
            return Task.CompletedTask;
        }

        public Task StartAsync(string ownerId)
        {
            if (!_spiderStatisticsDict.ContainsKey(ownerId))
            {
                _spiderStatisticsDict.TryAdd(ownerId, new SpiderStatistics());
            }

            _spiderStatisticsDict[ownerId].Start = DateTime.Now;
            return Task.CompletedTask;
        }

        public Task ExitAsync(string ownerId)
        {
            if (!_spiderStatisticsDict.ContainsKey(ownerId))
            {
                _spiderStatisticsDict.TryAdd(ownerId, new SpiderStatistics());
            }

            _spiderStatisticsDict[ownerId].Exit = DateTime.Now;
            return Task.CompletedTask;
        }

        public Task IncrementDownloadSuccessAsync(string agentId, int count, long elapsedMilliseconds)
        {
            if (!_downloadStatisticsDict.ContainsKey(agentId))
            {
                _downloadStatisticsDict.TryAdd(agentId, new DownloadStatistics());
            }

            _downloadStatisticsDict[agentId].AddSuccess(count);
            _downloadStatisticsDict[agentId].AddElapsedMilliseconds(elapsedMilliseconds);
            return Task.CompletedTask;
        }

        public Task IncrementDownloadFailedAsync(string agentId, int count)
        {
            if (!_downloadStatisticsDict.ContainsKey(agentId))
            {
                _downloadStatisticsDict.TryAdd(agentId, new DownloadStatistics());
            }

            _downloadStatisticsDict[agentId].AddFailed(count);
            return Task.CompletedTask;
        }

        public Task<List<DownloadStatistics>> GetDownloadStatisticsListAsync(int page, int size)
        {
            return Task.FromResult(_downloadStatisticsDict.Values.ToList());
        }

        public Task<DownloadStatistics> GetDownloadStatisticsAsync(string agentId)
        {
            var statistics = new DownloadStatistics();
            if (_downloadStatisticsDict.ContainsKey(agentId))
            {
                statistics = _downloadStatisticsDict[agentId];
            }

            return Task.FromResult(statistics);
        }

        public Task<SpiderStatistics> GetSpiderStatisticsAsync(string ownerId)
        {
            var statistics = new SpiderStatistics();
            if (_spiderStatisticsDict.ContainsKey(ownerId))
            {
                statistics = _spiderStatisticsDict[ownerId];
            }

            return Task.FromResult(statistics);
        }

        public Task<List<SpiderStatistics>> GetSpiderStatisticsListAsync(int page, int size)
        {
            return Task.FromResult(_spiderStatisticsDict.Values.ToList());
        }
    }
}