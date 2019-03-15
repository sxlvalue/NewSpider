using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewSpider.Downloader.Entity;

namespace NewSpider
{
    public interface IStatisticsService
    {
        Task SuccessAsync(string ownerId);

        Task FailedAsync(string ownerId);

        Task StartAsync(string ownerId);

        Task ExitAsync(string ownerId);

        Task DownloadSuccessAsync(string agentId, int count, long elapsedMilliseconds);

        Task DownloadFailedAsync(string agentId, int count);
        

        Task<List<DownloadStatistics>> GetDownloadStatisticsListAsync(int page, int size);
        

        Task<DownloadStatistics> GetDownloadStatisticsAsync(string agentId);
        

        Task<SpiderStatistics> GetSpiderStatisticsAsync(string ownerId);
        

        Task<List<DownloadStatistics>> GetSpiderStatisticsListAsync(int page, int size);
        
        Task TotalAsync(string ownerId, uint requestBatchCount);
    }
}