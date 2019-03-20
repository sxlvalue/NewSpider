using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data.Storage
{
    public abstract class StorageBase : DataFlowBase, IStorage
    {
        public override async Task<DataFlowResult> Handle(DataFlowContext context)
        {
            try
            {
                await Store(context);
                return DataFlowResult.Success;
            }
            catch (Exception e)
            {
                Logger?.LogError($"数据存储发生异常: {e}");
                return DataFlowResult.Failed;
            }
        }

        public abstract Task Store(DataFlowContext context);
    }
}