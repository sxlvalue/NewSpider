using System.Threading.Tasks;

namespace DotnetSpider.Data
{
    public interface IDataFlow
    {
        int Order { get; set; }
        
        Task<bool> Handle(DataFlowContext context);
    }
}