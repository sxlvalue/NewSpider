using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using DotnetSpider.Core;
using Newtonsoft.Json;

namespace DotnetSpider.Data.Storage.Model
{
    public class EntityBase<T> where T : class, new()
    {
        private Lazy<TableMetadata> _tableMetadata;

        public TableMetadata GetTableMetadata()
        {
            _tableMetadata = new Lazy<TableMetadata>();

            Configure();

            var type = typeof(T);

            var schema = type.GetCustomAttributes(typeof(Schema), false).FirstOrDefault();
            if (schema != null)
            {
                _tableMetadata.Value.Schema = (Schema) schema;
                if (string.IsNullOrWhiteSpace(_tableMetadata.Value.Schema.Table))
                {
                    _tableMetadata.Value.Schema = new Schema(_tableMetadata.Value.Schema.Database, type.Name);
                }
            }

            var properties = type.GetProperties().Where(x => x.CanRead && x.CanWrite).ToList();

            foreach (var property in properties)
            {
                var column = new Column
                {
                    Name = property.Name,
                    Type = property.PropertyType.Name,
                    Required = property.GetCustomAttributes(typeof(Required), false).Any()
                };

                var stringLength =
                    (StringLengthAttribute) property.GetCustomAttributes(typeof(StringLengthAttribute), false)
                        .FirstOrDefault();
                if (stringLength != null)
                {
                    column.Length = stringLength.MaximumLength;
                }

                _tableMetadata.Value.Columns.Add(property.Name, column);
            }

            // 如果未设置主键, 但实体中有名为 Id 的属性, 则默认把 Id 作为主键
            if (_tableMetadata.Value.Primary == null || _tableMetadata.Value.Primary.Count == 0)
            {
                var primary = properties.FirstOrDefault(x => x.Name.ToLower() == "id");
                if (primary != null)
                {
                    _tableMetadata.Value.Primary = new HashSet<string> {primary.Name};
                }
            }

            _tableMetadata.Value.TypeName = type.FullName;
 
            if (_tableMetadata.Value.Updates != null && _tableMetadata.Value.Updates.Count > 0 &&
                (_tableMetadata.Value.Primary == null || _tableMetadata.Value.Primary.Count == 0))
            {
                throw new SpiderException("更新数据依赖主键");
            }

            return _tableMetadata.Value;
        }

        protected virtual void Configure()
        {
        }

        protected T HasKey(Expression<Func<T, object>> indexExpression)
        {
            Check.NotNull(indexExpression, nameof(indexExpression));
            var columns = GetColumns(indexExpression);
            if (columns == null || columns.Count == 0)
            {
                throw new SpiderException("主键不能为空");
            }

            _tableMetadata.Value.Primary = new HashSet<string>(columns);
            return this as T;
        }

        protected T HasIndex(Expression<Func<T, object>> indexExpression, bool isUnique = false)
        {
            Check.NotNull(indexExpression, nameof(indexExpression));

            var columns = GetColumns(indexExpression);

            if (columns == null || columns.Count == 0)
            {
                throw new SpiderException("索引列不能为空");
            }

            _tableMetadata.Value.Indexes.Add(new IndexMetadata(columns.ToArray(), isUnique));
            return this as T;
        }

        protected T ConfigureUpdateColumns(Expression<Func<T, object>> indexExpression)
        {
            Check.NotNull(indexExpression, nameof(indexExpression));
            var columns = GetColumns(indexExpression);
            _tableMetadata.Value.Updates = columns;
            return this as T;
        }

        private HashSet<string> GetColumns(Expression<Func<T, object>> indexExpression)
        {
            var nodeType = indexExpression.Body.NodeType;
            var columns = new HashSet<string>();
            switch (nodeType)
            {
                case ExpressionType.New:
                {
                    var body = (NewExpression) indexExpression.Body;
                    foreach (var argument in body.Arguments)
                    {
                        var memberExpression = (MemberExpression) argument;
                        columns.Add(memberExpression.Member.Name);
                    }

                    if (columns.Count != body.Arguments.Count)
                    {
                        throw new SpiderException("表达式不正确");
                    }

                    break;
                }
                case ExpressionType.MemberAccess:
                {
                    var memberExpression = (MemberExpression) indexExpression.Body;
                    columns.Add(memberExpression.Member.Name);
                    break;
                }
                case ExpressionType.Convert:
                {
                    UnaryExpression body = (UnaryExpression) indexExpression.Body;
                    var memberExpression = (MemberExpression) body.Operand;
                    columns.Add(memberExpression.Member.Name);
                    break;
                }
                default:
                {
                    throw new SpiderException("表达式不正确");
                }
            }

            return columns;
        }
    }
}