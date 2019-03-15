using System.Collections.Generic;
using NewSpider.Downloader;

namespace NewSpider.Data
{
    public class DataFlowContext
    {
        public string Result { get; set; }
        
        public Dictionary<string,dynamic> Properties { get; set; }
    }
}