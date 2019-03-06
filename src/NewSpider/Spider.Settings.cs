using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NewSpider.Pipeline;
using NewSpider.Processor;

namespace NewSpider
{
    public partial class Spider
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        public Spider AddRequest(IRequest request)
        {
            request.OwnerId = Id;
            _requests.Add(request);
            if (_requests.Count == 1000)
            {
                _scheduler.PushAsync(Id, _requests);
                _logger.LogInformation("Push requests to scheduler");
                _requests.Clear();
            }
            return this;
        }
        
        public void AddProcessor(IPageProcessor processor)
        {
            _processors.Add(processor);
        }

        public void AddPipeline(IPipeline pipeline)
        {
            _pipelines.Add(pipeline);
        }

        public void AddDataFlow(IDataFlow dataFlow)
        {
            _dataFlows.Add(dataFlow);
        }
    }
}