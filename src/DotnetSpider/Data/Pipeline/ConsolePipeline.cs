using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DotnetSpider.Data.Pipeline
{
    public class ConsolePipeline : PipelineBase
    {
        protected override Task Process(Dictionary<string, List<dynamic>> items)
        {
            foreach (var item in items)
            {
                System.Console.WriteLine(item.Key + ":\t" + JsonConvert.SerializeObject(item.Value));
            }

            return Task.CompletedTask;
        }
    }
}