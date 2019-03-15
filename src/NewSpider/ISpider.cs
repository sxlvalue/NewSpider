using System.Threading.Tasks;
using NewSpider.Downloader;

namespace NewSpider
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

        uint Speed { get; set; }
    }
}