using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotnetSpider.Statistics
{
    public interface IStatisticsService
    {
        Task IncrementSuccessAsync(string ownerId);

        Task IncrementFailedAsync(string ownerId);

        Task IncrementTotalAsync(string ownerId, int count);
        
        Task StartAsync(string ownerId);

        Task ExitAsync(string ownerId);

        Task IncrementDownloadSuccessAsync(string agentId, int count, long elapsedMilliseconds);

        Task IncrementDownloadFailedAsync(string agentId, int count);

        Task PrintStatisticsAsync(string ownerId);
    }
}