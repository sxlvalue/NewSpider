using System.Threading.Tasks;

namespace NewSpider
{
    public interface IDataFlow
    {
        Task Handle(FlowContext context);
    }
}