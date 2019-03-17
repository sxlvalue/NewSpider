using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetSpider.Statistics
{
    public interface IStatisticsStore
    {
        Task IncrementSuccessAsync(string ownerId);

        Task IncrementFailedAsync(string ownerId);

        Task StartAsync(string ownerId);

        Task ExitAsync(string ownerId);

        Task IncrementDownloadSuccessAsync(string agentId, int count, long elapsedMilliseconds);

        Task IncrementDownloadFailedAsync(string agentId, int count);
        
        Task<List<DownloadStatistics>> GetDownloadStatisticsListAsync(int page, int size);

        Task<DownloadStatistics> GetDownloadStatisticsAsync(string agentId);      

        Task<SpiderStatistics> GetSpiderStatisticsAsync(string ownerId);
        
        Task<List<DownloadStatistics>> GetSpiderStatisticsListAsync(int page, int size);
        
        Task IncrementTotalAsync(string ownerId, int count);
    }
}