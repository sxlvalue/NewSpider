using System.Runtime.CompilerServices;
using NewSpider.Pipeline;
using NewSpider.Processor;

namespace NewSpider
{
    public partial class Spider
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        public Spider AddRequest(IRequest request)
        {
            _requests.Add(request);
            if (_requests.Count == 1000)
            {
                _scheduler.PushAsync(Id, _requests);
            }

            _requests.Clear();
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