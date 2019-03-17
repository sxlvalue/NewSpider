namespace DotnetSpider.Data.Processor
{
    public interface ILastPageChecker
    {
        bool IsLastPage(Page page);
    }
}