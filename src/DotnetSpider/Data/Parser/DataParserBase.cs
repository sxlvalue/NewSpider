using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetSpider.Downloader;
using DotnetSpider.Selector;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data.Parser
{
    public abstract class DataParserBase : DataFlowBase
    {
        public Func<Request, bool> CanParse { get; set; }

        /// <summary>
        /// 查询当前请求的下一级链接
        /// </summary>
        public Func<DataFlowContext, string[]> Follow { get; set; }

        public Func<DataFlowContext, ISelectable> Selectable { get; set; }

        public override async Task<DataFlowResult> HandleAsync(DataFlowContext context)
        {
            try
            {
                var response = context.GetResponse();
                var request = response.Request;
                // 如果不匹配则终止数据流程
                if (CanParse != null && !CanParse(request))
                {
                    return DataFlowResult.Terminated;
                }

                Selectable?.Invoke(context);

                var parserResult = await Parse(context);
                if (parserResult == DataFlowResult.Failed || parserResult == DataFlowResult.Terminated)
                {
                    return parserResult;
                }

                var urls = Follow?.Invoke(context);
                if (urls != null && urls.Length > 0)
                {
                    var followRequests = new List<Request>();
                    foreach (var url in urls)
                    {
                        var followRequest = CreateFromRequest(request, url);
                        if (CanParse(followRequest))
                        {
                            followRequests.Add(followRequest);
                        }
                    }

                    context.AddTargetRequests(followRequests.ToArray());
                }

                return DataFlowResult.Success;
            }
            catch (Exception e)
            {
                Logger?.LogError($"数据解析发生异常: {e}");
                return DataFlowResult.Failed;
            }
        }

        protected virtual Request CreateFromRequest(Request source, string url)
        {
            // TODO: 确认需要复制哪些字段
            var request = new Request {Url = url, Depth = source.Depth, Body = source.Body, Method = source.Method};
            request.AgentId = source.AgentId;
            request.RetriedTimes = 0;
            request.OwnerId = source.OwnerId;
            request.Properties = source.Properties;
            return request;
        }

        protected abstract Task<DataFlowResult> Parse(DataFlowContext context);
    }
}