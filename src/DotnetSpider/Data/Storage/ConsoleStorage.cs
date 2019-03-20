using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DotnetSpider.Data.Storage
{
    public class ConsoleStorage : StorageBase
    {
        public override Task Store(DataFlowContext context)
        {
            var items = context.GetItems();
            Console.WriteLine(JsonConvert.SerializeObject(items));
            return Task.CompletedTask;
        }
    }
}