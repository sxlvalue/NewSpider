using DotnetSpider.Downloader;

namespace DotnetSpider.Data.Processor
{
    public interface IPageFilter
    {
        bool Check(Request request);
    }
}