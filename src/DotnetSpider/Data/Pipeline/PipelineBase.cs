using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data.Pipeline
{
    public abstract class PipelineBase : IDataFlow
    {
        public int Order { get; set; }

        public ILogger Logger { get; set; }

        public async Task<bool> Handle(DataFlowContext context)
        {
            try
            {
                await Process(context);
                return true;
            }
            catch (Exception e)
            {
                Logger?.LogError($"数据管道发生异常: {e}");
                return false;
            }
        }

        protected abstract Task Process(DataFlowContext context);
    }
}