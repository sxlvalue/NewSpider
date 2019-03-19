using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetSpider.Downloader;
using DotnetSpider.Extraction;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DotnetSpider.Data.Processor
{
    public abstract class PageProcessorBase : IDataFlow
    {
        private IDataFlow _parser;
        
        protected PageProcessorBase(IDataFlow parser)
        {
            _parser = parser;
        }
        
        public int Order { get; set; }

        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger Logger { get; set; }

        public IPageFilter PageFilter { get; set; }

        public bool CleanPoundInTargetRequest { get; set; }

        public Func<DataFlowContext, ISelectable> Selectable { get; set; }

        public ITargetRequestResolver TargetRequestResolver { get; set; }

        public async Task<DataFlowResult> Handle(DataFlowContext context)
        {
            try
            {
                var response = context.GetResponse();
                if (response == null)
                {
                    Logger.LogError("未找到回复信息，无法解析数据");
                    return DataFlowResult.Failed;
                }

                var request = response.Request;
                if (request == null)
                {
                    Logger.LogError("未找到请求信息，无法解析数据");
                    return DataFlowResult.Failed;
                }

                // 如果不匹配则终止数据流程
                if (PageFilter != null && !PageFilter.Check(request))
                {
                    return DataFlowResult.Terminated;
                }

                _parser.Logger = Logger;
                
                var result =await _parser.Handle(context);

                var requests = TargetRequestResolver?.Resolver(selectable);
                if (requests != null && requests.Length > 0)
                {
                    var validRequests = new List<Request>();
                    foreach (var _ in requests)
                    {
                        if (PageFilter != null && !PageFilter.Check(request)) continue;

                        validRequests.Add(CleanPoundInTargetRequest
                            ? CreateNewRequest(request, _.Split('#')[0])
                            : CreateNewRequest(request, _));
                    }

                    context.AddTargetRequests(validRequests.ToArray());
                }

                return DataFlowResult.Success;
            }
            catch (Exception e)
            {
                Logger?.LogError($"数据解析发生异常: {e}");
                return DataFlowResult.Failed;
            }
        }

        protected virtual Request CreateNewRequest(Request source, string url)
        {
            var request = new Request {Url = url, Depth = source.Depth, Body = source.Body, Method = source.Method};
            request.AgentId = source.AgentId;
            request.RetriedTimes = 0;
            request.OwnerId = source.OwnerId;
            return request;
        }

        protected abstract Task<Dictionary<string, List<dynamic>>> Process(ISelectable selectable);
    }
}