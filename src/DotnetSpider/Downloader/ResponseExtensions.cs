using System;
using DotnetSpider.Core;
using DotnetSpider.Selector;
using Newtonsoft.Json;

namespace DotnetSpider.Downloader
{
    public static class ResponseExtensions
    {
        public static ISelectable ToSelectable(this Response response,
            ContentType type = ContentType.Auto, bool removeOutboundLinks = true)
        {
            switch (type)
            {
                case ContentType.Auto:
                {
                    return IsJson(response.Content)
                        ? new Selectable(response.Content)
                        : new Selectable(response.Content, response.Request.Url, removeOutboundLinks);
                }
                case ContentType.Html:
                {
                    return new Selectable(response.Content, response.Request.Url, removeOutboundLinks);
                }
                case ContentType.Json:
                {
                    if (IsJson(response.Content))
                    {
                        return new Selectable(response.Content);
                    }
                    else
                    {
                        throw new SpiderException("内容不是合法的 Json");
                    }
                }
                default:
                {
                    throw new NotSupportedException();
                }
            }
        }

        private static bool IsJson(string content)
        {
            try
            {
                JsonConvert.DeserializeObject(content);
                return true;
            }
            catch
            {
                //TODO: maybe we need log here?
                return false;
            }
        }
    }
}