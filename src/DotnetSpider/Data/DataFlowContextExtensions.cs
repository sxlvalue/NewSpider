using System.Collections.Generic;
using DotnetSpider.Core;
using DotnetSpider.Downloader;
using DotnetSpider.Selector;

namespace DotnetSpider.Data
{
    public static class DataFlowContextExtensions
    {
        private const string RequestKey = "DotnetSpider-Request";
        private const string ResponseKey = "DotnetSpider-Response";
        private const string SelectableKey = "DotnetSpider-Selectable";
        private const string ExtractedRequestsKey = "DotnetSpider-ExtractedRequests";

        public static void AddResponse(this DataFlowContext context, Response response)
        {
            context[ResponseKey] = response;
        }

        public static Response GetResponse(this DataFlowContext context)
        {
            return context[ResponseKey];
        }

        public static void AddTargetRequests(this DataFlowContext context, params Request[] requests)
        {
            if (!context.Contains(RequestKey))
            {
                context[ExtractedRequestsKey] = new List<Request>(requests);
            }
            else
            {
                context[ExtractedRequestsKey].AddRange(requests);
            }
        }

        public static List<Request> GetTargetRequests(this DataFlowContext context)
        {
            return context[ExtractedRequestsKey];
        }

        public static ISelectable GetSelectable(this DataFlowContext context,
            ContentType contentType = ContentType.Auto, bool removeOutboundLinks = true)
        {
            if (!context.Contains(SelectableKey))
            {
                var response = GetResponse(context);

                context[SelectableKey] = response?.ToSelectable(contentType, removeOutboundLinks);
            }

            return context[SelectableKey];
        }
    }
}