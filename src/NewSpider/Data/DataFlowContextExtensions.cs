using System.Collections.Generic;
using NewSpider.Downloader;

namespace NewSpider.Data
{
    public static class DataFlowContextExtensions
    {
        private const string RequestKey = "Request";

        public static void AddRequest(this DataFlowContext context, Request request)
        {
            if (context.Properties == null)
            {
                context.Properties = new Dictionary<string, dynamic>();
            }

            if (!context.Properties.ContainsKey(RequestKey))
            {
                context.Properties.Add(RequestKey, request);
            }
            else
            {
                context.Properties[RequestKey] = request;
            }
        }

        public static Request GetRequest(this DataFlowContext context)
        {
            if (context.Properties == null)
            {
                return null;
            }

            if (!context.Properties.ContainsKey(RequestKey))
            {
                return null;
            }
            else
            {
                return context.Properties[RequestKey];
            }
        }
    }
}