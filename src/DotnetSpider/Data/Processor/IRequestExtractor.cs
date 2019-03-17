using DotnetSpider.Downloader;

namespace DotnetSpider.Data.Processor
{
    public interface IRequestExtractor
    {
        Request[] Extract(Page page);
    }
}