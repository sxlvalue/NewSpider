using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data
{
    public abstract class DataFlowBase : IDataFlow
    {
        public ILogger Logger { get; set; }

        public virtual Task InitAsync()
        {
            return Task.CompletedTask;
        }

        public abstract Task<DataFlowResult> HandleAsync(DataFlowContext context);

        public virtual void Dispose()
        {
        }
    }
}