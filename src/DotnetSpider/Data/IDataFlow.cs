using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data
{
    public interface IDataFlow
    {
        int Order { get; set; }
        
        ILogger Logger { get; set; }
        
        Task<DataFlowResult> Handle(DataFlowContext context);
    }
}