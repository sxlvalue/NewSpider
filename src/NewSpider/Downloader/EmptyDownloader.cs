using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NewSpider.Downloader
{
    public class EmptyDownloader : AbstractDownloader
    {
        protected override Task<Response> ImplDownloadAsync(Request request)
        {
            return Task.FromResult(new Response
            {
                Request = request,
                Content = "From empty downloader"
            });
        }
    }
}