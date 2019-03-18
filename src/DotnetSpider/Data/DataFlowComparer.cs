using System.Collections.Generic;

namespace DotnetSpider.Data
{
    public class DataFlowComparer : IComparer<IDataFlow>
    {
        public int Compare(IDataFlow x, IDataFlow y)
        {
            Check.NotNull(x, nameof(x));
            Check.NotNull(y, nameof(y));
            return x.Order - y.Order;
        }
    }
}