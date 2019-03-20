using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DotnetSpider.Data.Storage
{
    public class ConsoleStorage : StorageBase
    {
        protected override Task<DataFlowResult> Store(DataFlowContext context)
        {
            var items = context.GetItems();
            Console.WriteLine(JsonConvert.SerializeObject(items));
            return Task.FromResult(DataFlowResult.Success);
        }
    }
}