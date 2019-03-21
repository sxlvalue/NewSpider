using System;
using System.Reflection;
using System.Runtime.Serialization;
using DotnetSpider.Selector;

namespace DotnetSpider.Data.Parser.Attribute
{
    /// <summary>
    /// 属性选择器的定义
    /// </summary>
    public class ValueSelector : Selector
    {
        internal PropertyInfo Property { get; set; }

        /// <summary>
        /// 构造方法
        /// </summary>
        public ValueSelector()
        {
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="type">选择器类型</param>
        /// <param name="expression">表达式</param>
        public ValueSelector(string expression, SelectorType type = SelectorType.XPath)
            : base(expression, type)
        {
        }

        /// <summary>
        /// 数据格式化
        /// </summary>
        public Formatter.Formatter[] Formatters { get; set; }
        
        /// <summary>
        /// 额外选项的定义
        /// </summary>
        public ValueOption ValueOption { get; set; }
    }
}