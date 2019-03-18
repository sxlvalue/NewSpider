using System.Collections.Generic;
using System.Linq;
using DotnetSpider.Downloader;
using DotnetSpider.Extraction;

namespace DotnetSpider.Data.Processor
{
    public class XpathTargetRequestResolver : ITargetRequestResolver
    {
        private readonly string[] _xPaths;

        public XpathTargetRequestResolver(params string[] xPaths)
        {
            _xPaths = xPaths;
        }

        public string[] Resolver(ISelectable selectable)
        {
            var urls = new List<string>();
            foreach (var xpath in _xPaths)
            {
                var links = selectable.XPath(xpath).Links().GetValues();
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
        }
    }
}