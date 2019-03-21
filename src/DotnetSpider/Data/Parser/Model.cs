using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotnetSpider.Data.Parser.Attribute;
using DotnetSpider.Data.Storage.Model;
using DotnetSpider.Selector;
using Newtonsoft.Json;

namespace DotnetSpider.Data.Parser
{
    public class Model<T> where T : EntityBase<T>, new()
    {
        /// <summary>
        /// 数据模型的选择器
        /// </summary>
        public Attribute.Selector Selector { get; protected set; }

        /// <summary>
        /// 从最终解析到的结果中取前 Take 个实体
        /// </summary>
        public int Take { get; protected set; }

        /// <summary>
        /// 设置 Take 的方向, 默认是从头部取
        /// </summary>
        public bool TakeFromHead { get; protected set; }

        /// <summary>
        /// 爬虫实体定义的数据库列信息
        /// </summary>
        public HashSet<ValueSelector> ValueSelectors { get; protected set; }

        /// <summary>
        /// 目标链接的选择器
        /// </summary>
        public HashSet<FollowSelector> FollowSelectors { get; protected set; }

        /// <summary>
        /// 共享值的选择器
        /// </summary>
        public HashSet<ValueSelector> ShareValueSelectors { get; protected set; }

        public Model()
        {
            var type = typeof(T);
            var entitySelector =
                type.GetCustomAttributes(typeof(EntitySelector), true).FirstOrDefault() as EntitySelector;
            int take = 0;
            bool takeFromHead = true;
            Attribute.Selector selector = null;
            if (entitySelector != null)
            {
                take = entitySelector.Take;
                takeFromHead = entitySelector.TakeFromHead;
                selector = new Attribute.Selector {Expression = entitySelector.Expression, Type = entitySelector.Type};
            }

            var targets = type.GetCustomAttributes(typeof(FollowSelector), true).Select(s => (FollowSelector) s)
                .ToList();
            var sharedValueSelectors = type.GetCustomAttributes(typeof(ValueSelector), true).Select(e =>
            {
                var p = (ValueSelector) e;
                return new ValueSelector
                {
                    Expression = p.Expression,
                    Type = p.Type
                };
            }).ToList();

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var fields = new HashSet<ValueSelector>();
            foreach (var property in properties)
            {
                var field = property.GetCustomAttributes(typeof(ValueSelector), true).FirstOrDefault() as ValueSelector;

                if (field == null)
                {
                    continue;
                }

                field.Property = property;
                field.Formatters = property.GetCustomAttributes(typeof(Formatter.Formatter), true)
                    .Select(p => (Formatter.Formatter) p).ToArray();
                fields.Add(field);
            }

            Selector = selector;
            ValueSelectors = fields;
            FollowSelectors = new HashSet<FollowSelector>(targets);
            ShareValueSelectors = new HashSet<ValueSelector>(sharedValueSelectors);
            Take = take;
            TakeFromHead = takeFromHead;
        }
    }
}