using System.Collections.Generic;
using DotnetSpider.Core;
using DotnetSpider.Downloader;
using DotnetSpider.Extraction;

namespace DotnetSpider.Data
{
    public static class DataFlowContextExtensions
    {
        private const string RequestKey = "DotnetSpider-Request";
        private const string ResponseKey = "DotnetSpider-Response";
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
            if (!context.ContainsKey(RequestKey))
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

        public static ISelectable CreateSelectable(this DataFlowContext context,
            ContentType contentType = ContentType.Auto, bool removeOutboundLinks = true)
        {
            var response = GetResponse(context);

            return response?.ToSelectable(contentType, removeOutboundLinks);
        }
    }
}