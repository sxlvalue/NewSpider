using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetSpider.Downloader;
using DotnetSpider.Extraction;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data.Processor
{
    public class DefaultPageProcessor : PageProcessorBase
    {
        protected override Task<Dictionary<string, List<dynamic>>> Process(ISelectable selectable)
        {
            var result = new
            {
                Title = selectable.XPath("//title").GetValue(),
                // Html = selectable.GetValue(ValueOption.OuterHtml),
                Url = selectable.Environment("URL")
            };
            return Task.FromResult(new Dictionary<string, List<dynamic>>
            {
                {"DotnetSpider-DataItems", new List<dynamic> {result}}
            });
        }
    }
}