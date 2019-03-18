using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetSpider.Downloader;
using DotnetSpider.Extraction;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data.Processor
{
    public abstract class PageProcessorBase : IDataFlow
    {
        public int Order { get; set; }

        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// 配置 PageProcessor 是否对深度为1的链接进行正则筛选
        /// </summary>
        public bool IgnoreFilterDefaultRequest { get; set; } = true;

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

                if (!(request.Depth == 1 && !IgnoreFilterDefaultRequest))
                {
                    // 如果不匹配则终止数据流程
                    if (PageFilter != null && !PageFilter.Check(request))
                    {
                        return DataFlowResult.Terminated;
                    }
                }

                if (Selectable == null)
                {
                    Selectable = c => c.GetSelectable();
                }

                var selectable = Selectable(context);
                //TODO: 添加更多的属性
                selectable.Properties.Add("URL", request.Url);

                var items = await Process(selectable);
                context.AddDataItems(items);

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