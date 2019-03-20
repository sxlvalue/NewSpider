using System.Runtime.CompilerServices;
using DotnetSpider.Core;
using DotnetSpider.Data;
using DotnetSpider.Data.Parser;
using DotnetSpider.Data.Storage;
using DotnetSpider.Downloader;
using Microsoft.Extensions.Logging;

namespace DotnetSpider
{
    public partial class Spider
    {
        public void AddDataParser(IDataParser parser)
        {
            Check.NotNull(parser, nameof(parser));

            if (parser.Order < DataParserComparer || parser.Order > StorageComparer)
            {
                parser.Order = DataParserComparer;
            }

            parser.Logger = _loggerFactory.CreateLogger(parser.GetType());
            _dataFlows.Add(parser);
        }

        public void AddStorage(IStorage storage)
        {
            Check.NotNull(storage, nameof(storage));

            if (storage.Order < StorageComparer)
            {
                storage.Order = StorageComparer;
            }

            storage.Logger = _loggerFactory.CreateLogger(storage.GetType());
            _dataFlows.Add(storage);
        }

        public void AddDataFlow(IDataFlow dataFlow)
        {
            if (dataFlow.Order < 0)
            {
                throw new DotnetSpiderException("排序标识必须大于或等于 2");
            }

            dataFlow.Logger = _loggerFactory.CreateLogger(dataFlow.GetType());

            _dataFlows.Add(dataFlow);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public ISpider AddRequests(params Request[] requests)
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
        public ISpider AddRequests(params string[] urls)
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