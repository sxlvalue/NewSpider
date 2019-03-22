using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DotnetSpider.Data.Storage
{
    public class ConsoleEntityStorage : StorageBase
    {
        protected override Task<DataFlowResult> Store(DataFlowContext context)
        {
            var items = context.GetItems();
            if (items == null || items.Count == 0)
            {
                return Task.FromResult(DataFlowResult.Success);
            }

            foreach (var item in items)
            {
                foreach (var data in item.Value)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(data));
                }
            }

            return Task.FromResult(DataFlowResult.Success);
        }
    }
}