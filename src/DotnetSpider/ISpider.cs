using System.Threading.Tasks;
using DotnetSpider.Downloader;

namespace DotnetSpider
{
    public interface ISpider
    {
        string Id { get; set; }

        string Name { get; set; }

        void Pause();

        void Continue();

        void Exit();

        Task RunAsync();

        ISpider AddRequests(params Request[] requests);

        double Speed { get; set; }
    }
}