using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data
{
    public abstract class DataFlowBase : IDataFlow
    {
        public ILogger Logger { get; set; }

        public abstract Task<DataFlowResult> Handle(DataFlowContext context);

        public virtual void Dispose()
        {
        }
    }
}