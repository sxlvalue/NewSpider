using System.Threading.Tasks;

namespace DotnetSpider.Downloader
{
    public class EmptyDownloader : AbstractDownloader
    {
        protected override Task<Response> ImplDownloadAsync(Request request)
        {
            return Task.FromResult(new Response
            {
                Request = request,
                RawText = "From empty downloader"
            });
        }
    }
}