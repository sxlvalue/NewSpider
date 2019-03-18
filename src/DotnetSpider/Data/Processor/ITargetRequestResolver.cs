using DotnetSpider.Downloader;
using DotnetSpider.Extraction;

namespace DotnetSpider.Data.Processor
{
    public interface ITargetRequestResolver
    {
        string[] Resolver(ISelectable selectable);
    }
}