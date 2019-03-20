using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DotnetSpider.Data.Storage
{
    public class JsonFileStorage : FileStorageBase
    {
        protected override async Task<DataFlowResult> Store(DataFlowContext context)
        {
            var response = context.GetResponse();

            var file = Path.Combine(GetDataFolder(response.Request.OwnerId), $"{response.Request.Hash}.json");
            CreateFile(file);
            var items = context.GetItems();
            await Writer.WriteLineAsync(JsonConvert.SerializeObject(items));
            return DataFlowResult.Success;
        }
    }
}