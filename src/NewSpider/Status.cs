using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NewSpider
{
    /// <summary>
    /// 爬虫状态
    /// </summary>
    [System.Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Status
    {
        /// <summary>
        /// 初始化
        /// </summary>
        Init = 1,

        /// <summary>
        /// 正在运行
        /// </summary>
        Running = 2,

        /// <summary>
        /// 暂停
        /// </summary>
        Paused = 4,

        /// <summary>
        /// 完成
        /// </summary>
        Exited = 8
    }
}