using System.Collections.Generic;

namespace DotnetSpider.Data.Storage.Model
{
    public class TableMetadata
    {
        public string TypeName { get; set; }
        
        public Schema Schema { get; set; }

        public HashSet<string> Primary { get; set; }

        public HashSet<IndexMetadata> Indexes { get; }

        public Dictionary<string, string> Columns { get; }

        public TableMetadata()
        {
            Indexes = new HashSet<IndexMetadata>();
            Columns = new Dictionary<string, string>();
        }
    }
}