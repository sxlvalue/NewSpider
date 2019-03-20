using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotnetSpider.Downloader;

namespace DotnetSpider.Data.Parser
{
    public class DataParser : DataParserBase
    {
        protected override Task<DataFlowResult> Parse(DataFlowContext context)
        {
            var response = context.GetResponse();
            context.AddItem("URL", response.Request.Url);
            context.AddItem("Content", response.Content);
            return Task.FromResult(DataFlowResult.Success);
        }

        public static Func<DataFlowContext, string[]> XpathFollow(params string[] xpaths)
        {
            return context =>
            {
                var urls = new List<string>();
                foreach (var xpath in xpaths)
                {
                    var links = context.GetSelectable().XPath(xpath).Links().GetValues();
                    foreach (var link in links)
                    {
#if !NETSTANDARD
                        urls.Add(System.Web.HttpUtility.HtmlDecode(System.Web.HttpUtility.UrlDecode(link)));
#else
                        urls.Add(System.Net.WebUtility.HtmlDecode(System.Net.WebUtility.UrlDecode(link)));
#endif
                    }
                }

                return urls.ToArray();
            };
        }

        public static Func<Request, bool> RegexCanParse(params string[] patterns)
        {
            return request =>
            {
                foreach (var pattern in patterns)
                {
                    if (Regex.IsMatch(request.Url, pattern))
                    {
                        return true;
                    }
                }

                return false;
            };
        }
    }
}