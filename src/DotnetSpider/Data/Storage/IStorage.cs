using System.Threading.Tasks;

namespace DotnetSpider.Data.Storage
{
    public interface IStorage : IDataFlow
    {
        Task Store(DataFlowContext context);
    }
}