using System.Threading.Tasks;

namespace NewSpider.Data
{
    public interface IDataFlow
    {
        Task<bool> Handle(DataFlowContext context);
    }
}