using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data.Processor
{
    public abstract class PageProcessorBase : IDataFlow
    {
        public int Order { get; set; }

        /// <summary>
        /// 日志接口
        /// </summary>
        public ILogger Logger { get; set; }
        
        /// <summary>
        /// 用于判断是否需要处理当前 Request, 以及解析出来的目标链接是否需要添加到队列.
        /// RequestExtractor 解析出来的结果也需验证是否符合 Filter, 如果不符合 Filter 那么最终也不会进入到 Processor, 即为无意义的 Request
        /// </summary>
        public IPageFilter Filter { get; set; }
        
        /// <summary>
        /// 是否最后一页的判断接口, 如果是最后一页, 则不需要执行 RequestExtractor
        /// </summary>
        public ILastPageChecker LastPageChecker { get; set; }
        
        /// <summary>
        /// 去掉链接#后面的所有内容
        /// </summary>
        public bool CleanPound { get; set; }
        
        /// <summary>
        /// 是否去除外链
        /// </summary>
        public bool RemoveOutboundLinks { get; set; }

        public async Task<bool> Handle(DataFlowContext context)
        {
            try
            {
                await Process(context);
                return true;
            }
            catch (Exception e)
            {
                Logger?.LogError($"数据解析发生异常: {e}");
                return false;
            }
        }

        protected abstract Task Process(DataFlowContext context);
    }
}