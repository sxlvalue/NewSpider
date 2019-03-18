using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data.Processor
{
    public class NullPageProcessor: IDataFlow
    {
        public int Order { get; set; }
        
        public ILogger Logger { get; set; }
        
        public Task<DataFlowResult> Handle(DataFlowContext context)
        {
            Console.WriteLine("You used a null processor.");
            return Task.FromResult(DataFlowResult.Success);
        }
    }
}