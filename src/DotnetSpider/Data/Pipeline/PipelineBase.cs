using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data.Pipeline
{
    public abstract class PipelineBase : IDataFlow
    {
        public int Order { get; set; }

        public ILogger Logger { get; set; }

        public async Task<DataFlowResult> Handle(DataFlowContext context)
        {
            try
            {
                await Process(context.DataItems);
                return DataFlowResult.Success;
            }
            catch (Exception e)
            {
                Logger?.LogError($"数据管道发生异常: {e}");
                return DataFlowResult.Failed;
            }
        }

        protected abstract Task Process(Dictionary<string, List<dynamic>> items);
    }
}