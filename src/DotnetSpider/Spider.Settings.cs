using System.Runtime.CompilerServices;
using DotnetSpider.Core;
using DotnetSpider.Data;
using DotnetSpider.Data.Pipeline;
using DotnetSpider.Data.Processor;
using DotnetSpider.Downloader;
using Microsoft.Extensions.Logging;

namespace DotnetSpider
{
    public partial class Spider
    {
        public void AddProcessor(PageProcessorBase processor)
        {
            processor.Order = ProcessComparer;
            _dataFlows.Add(processor);
        }

        public void AddPipeline(PipelineBase pipeline)
        {
            pipeline.Order = PipelineComparer;
            _dataFlows.Add(pipeline);
        }

        public void AddDataFlow(IDataFlow dataFlow)
        {
            if (dataFlow.Order < 2)
            {
                throw new DotnetSpiderException("排序标识必须大于或等于 2");
            }

            _dataFlows.Add(dataFlow);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public ISpider AddRequests(params Request[] requests)
        {
            foreach (var request in requests)
            {
                request.OwnerId = Id;
                _requests.Add(request);
                if (_requests.Count % EnqueueBatchCount == 0)
                {
                    PushRequests();
                }
            }

            return this;
        }

        private void PushRequests()
        {
            if (_requests.Count <= 0) return;

            var count = _scheduler.Enqueue(_requests);
            _statisticsService.IncrementTotalAsync(Id, count).ConfigureAwait(false);
            _logger.LogInformation($"任务 {Id} 请求推送到调度器: {_requests.Count}");
            _requests.Clear();
        }
    }
}