using System.Threading.Tasks;
using DotnetSpider.Data.Storage.Model;

namespace DotnetSpider.Data.Storage
{
    public class EntityStorageBase : StorageBase
    {
        protected override Task<DataFlowResult> Store(DataFlowContext context)
        {
            var items = context.GetItems();

            foreach (var item in items)
            {
                var tableMetadata = context[item.Key];
            }

            return Task.FromResult(DataFlowResult.Success);
        }
    }
}