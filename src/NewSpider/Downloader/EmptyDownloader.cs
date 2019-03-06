using System.Threading.Tasks;

namespace NewSpider.Downloader
{
    public class EmptyDownloader : IDownloader
    {
        public Task<Response> DownloadAsync(Request request)
        {
            return Task.FromResult(new Response
            {
                Request = request,
                Content = "hahaha"
            });            
        }
    }
}