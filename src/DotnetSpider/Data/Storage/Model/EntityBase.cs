using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DotnetSpider.Core;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace DotnetSpider.Data.Storage.Model
{
    public class EntityBase<T> where T : class, new()
    {
        private Lazy<TableMetadata> _tableMetadata;

        public void SetSchema(string database, string table)
        {
            _tableMetadata.Value.Schema = new Schema(database, table);
        }

        public TableMetadata GetTableMetadata()
        {
            _tableMetadata = new Lazy<TableMetadata>();
            Configure();
            var type = typeof(T);
            var properties = type.GetProperties().Where(x => x.CanRead && x.CanWrite).ToList();
            foreach (var property in properties)
            {
                _tableMetadata.Value.Columns.Add(property.Name, property.PropertyType.Name);
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