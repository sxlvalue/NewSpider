using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewSpider.Statistics
{
    public class MemoryStatisticsStore : IStatisticsStore
    {
        private readonly ConcurrentDictionary<string, SpiderStatistics> _spiderStatisticsDict =
            new ConcurrentDictionary<string, SpiderStatistics>();

        private readonly ConcurrentDictionary<string, DownloadStatistics> _downloadStatisticsDict =
            new ConcurrentDictionary<string, DownloadStatistics>();

        public Task IncrementTotalAsync(string ownerId, uint count)
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

        public Task IncrementFailedAsync(string ownerId)
        {
            if (!_spiderStatisticsDict.ContainsKey(ownerId))
            {
                _spiderStatisticsDict.TryAdd(ownerId, new SpiderStatistics());
            }

            _spiderStatisticsDict[ownerId].IncFailed();
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
            throw new System.NotImplementedException();
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

        public Task<List<DownloadStatistics>> GetSpiderStatisticsListAsync(int page, int size)
        {
            throw new System.NotImplementedException();
        }
    }
}