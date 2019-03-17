using DotnetSpider.Downloader;

namespace DotnetSpider.Data.Processor
{
    public interface IPageFilter
    {
        bool IsMatch(Request request);
    }
}