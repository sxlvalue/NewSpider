using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DotnetSpider.Data.Storage
{
    public class FileStorage : FileStorageBase
    {
        public FileStorage(string folder = null) : base(folder)
        {
        }

        protected override async Task<DataFlowResult> Store(DataFlowContext context)
        {
            var response = context.GetResponse();

            var file = Path.Combine(GetDataFolder(response.Request.OwnerId), $"{response.Request.Hash}.data");
            CreateFile(file);

            await Writer.WriteLineAsync("URL:\t" + response.Request.Url);
            var items = context.GetItems();
            await Writer.WriteLineAsync("DATA:\t" + JsonConvert.SerializeObject(items));

            return DataFlowResult.Success;
        }
    }
}