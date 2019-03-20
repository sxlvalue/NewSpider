using System.Threading.Tasks;

namespace DotnetSpider.Data.Parser
{
    public interface IDataParser : IDataFlow
    {
        Task<DataFlowResult> Parse(DataFlowContext context);
    }
}