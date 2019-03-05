using System.Threading.Tasks;

namespace NewSpider
{
    public interface ISpider
    {
        void Pause();


        void Continue();


        void Exit();

        Task RunAsync();
    }
}