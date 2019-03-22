using System.Collections.Generic;
using System.Linq;

namespace DotnetSpider.Data.Storage.Model
{
    public class TableMetadata
    {
        public string TypeName { get; set; }

        public Schema Schema { get; set; }

        public HashSet<string> Primary { get; set; }

        public HashSet<IndexMetadata> Indexes { get; }
        
        public HashSet<string> Updates { get; set; }

        /// <summary>
        /// 属性名，属性数据类型的字典
        /// </summary>
        public Dictionary<string, Column> Columns { get; }

        internal bool IsAutoIncrementPrimary => Primary.Count == 1 &&
                                                (Columns[Primary.First()].Type == "Int32" ||
                                                 Columns[Primary.First()].Type == "Int64");

        public TableMetadata()
        {
            Indexes = new HashSet<IndexMetadata>();
            Columns = new Dictionary<string, Column>();
        }
    }
}