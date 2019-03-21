using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data
{
    public interface IDataFlow : System.IDisposable
    {
        ILogger Logger { get; set; }

        Task InitAsync();

        Task<DataFlowResult> HandleAsync(DataFlowContext context);
    }
}