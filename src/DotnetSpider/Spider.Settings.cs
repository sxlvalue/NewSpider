using System.Runtime.CompilerServices;
using DotnetSpider.Data;
using DotnetSpider.Downloader;
using Microsoft.Extensions.Logging;

namespace DotnetSpider
{
    public partial class Spider
    {
        public Spider AddDataFlow(IDataFlow dataFlow)
        {
            CheckIfRunning();
            dataFlow.Logger = _loggerFactory.CreateLogger(dataFlow.GetType());
            _dataFlows.Add(dataFlow);
            return this;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Spider AddRequests(params Request[] requests)
        {
            foreach (var request in requests)
            {
                request.OwnerId = Id;
                request.Depth = 1;
                _requests.Add(request);
                if (_requests.Count % EnqueueBatchCount == 0)
                {
                    EnqueueRequests();
                }
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Spider AddRequests(params string[] urls)
        {
            foreach (var url in urls)
            {
                var request = new Request {Url = url, OwnerId = Id, Depth = 1};
                _requests.Add(request);
                if (_requests.Count % EnqueueBatchCount == 0)
                {
                    EnqueueRequests();
                }
            }

            return this;
        }

        private void EnqueueRequests()
        {
            if (_requests.Count <= 0) return;

            var count = _scheduler.Enqueue(_requests);
            _statisticsService.IncrementTotalAsync(Id, count).ConfigureAwait(false);
            _logger.LogInformation($"任务 {Id} 请求推送到调度器: {_requests.Count}");
            _requests.Clear();
        }
    }
}