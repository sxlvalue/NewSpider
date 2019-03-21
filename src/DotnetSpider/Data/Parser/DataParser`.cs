using System.Threading.Tasks;
using DotnetSpider.Data.Storage.Model;

namespace DotnetSpider.Data.Parser
{
    public class DataParser<T> : DataParser where T : EntityBase<T>, new()
    {
        private readonly Model<T> _model;

        public DataParser()
        {
            _model = new Model<T>();
        }

        protected override Task<DataFlowResult> Parse(DataFlowContext context)
        {
            var response = context.GetResponse();
            context.AddItem("URL", response.Request.Url);
            context.AddItem("Content", response.Content);
            return Task.FromResult(DataFlowResult.Success);
        }
    }
}