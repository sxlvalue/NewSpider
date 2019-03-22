using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DotnetSpider.Data.Storage.Model;
using DotnetSpider.Selector;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Data.Parser
{
    public class DataParser<T> : DataParser where T : EntityBase<T>, new()
    {
        private readonly Model<T> _model;
        private readonly TableMetadata _tableMetadata;

        public DataParser()
        {
            _model = new Model<T>();
            _tableMetadata = new T().GetTableMetadata();
        }

        protected override Task<DataFlowResult> Parse(DataFlowContext context)
        {
            if (!context.Contains(_model.TypeName))
            {
                context.Add(_model.TypeName, _tableMetadata);
            }

            var selectable = context.GetSelectable();
            List<dynamic> results = new List<dynamic>();
            if (selectable.Properties == null)
            {
                selectable.Properties = new Dictionary<string, object>();
            }

            var environments = new Dictionary<string, string>();
            foreach (var property in context.GetResponse().Request.Properties)
            {
                environments.Add(property.Key, property.Value);
            }

            if (_model.ShareValueSelectors != null)
            {
                foreach (var selector in _model.ShareValueSelectors)
                {
                    string name = selector.Name;
                    var value = selectable.Select(selector.ToSelector()).GetValue();
                    if (!environments.ContainsKey(name))
                    {
                        environments.Add(name, value);
                    }
                    else
                    {
                        environments[name] = value;
                    }
                }
            }

            bool singleExtractor = _model.Selector == null;

            if (!singleExtractor)
            {
                var selector = _model.Selector.ToSelector();

                var list = selectable.SelectList(selector).Nodes()?.ToList();
                if (list != null)
                {
                    if (_model.Take > 0 && list.Count > _model.Take)
                    {
                        list = _model.TakeFromHead
                            ? list.Take(_model.Take).ToList()
                            : list.Skip(list.Count - _model.Take).ToList();
                    }

                    for (var i = 0; i < list.Count; ++i)
                    {
                        var item = list.ElementAt(i);
                        var obj = ParseObject(environments, item, i);
                        if (obj != null)
                        {
                            results.Add(obj);
                        }
                        else
                        {
                            Logger?.LogWarning($"解析到空数据，类型: {_model.TypeName}");
                        }
                    }
                }
            }
            else
            {
                var obj = ParseObject(environments, selectable, 0);
                if (obj != null)
                {
                    results.Add(obj);
                }
                else
                {
                    Logger?.LogWarning($"解析到空数据，类型: {_model.TypeName}");
                }
            }

            if (results.Count > 0)
            {
                var items = context.GetItem(_model.TypeName);
                if (items == null)
                {
                    context.AddItem(_model.TypeName, results);
                }
                else
                {
                    items.AddRange(results);
                }
            }

            return Task.FromResult(DataFlowResult.Success);
        }

        private T ParseObject(Dictionary<string, string> environments, ISelectable selectable,
            int index)
        {
            var dataObject = new T();

            foreach (var field in _model.ValueSelectors)
            {
                string value = null;
                if (field.Type == SelectorType.Enviroment)
                {
                    if (field.Expression == "INDEX")
                    {
                        value = index.ToString();
                    }
                    else if (field.Expression == "GUID")
                    {
                        value = Guid.NewGuid().ToString();
                    }
                    else if (field.Expression == "DATE")
                    {
                        value = DateTime.Now.Date.ToString("yyyy-MM-dd");
                    }
                    else if (field.Expression == "DATETIME")
                    {
                        value = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                    }
                    else
                    {
                        if (environments.ContainsKey(field.Expression))
                        {
                            value = environments[field.Expression];
                        }
                    }
                }
                else
                {
                    var selector = field.ToSelector();
                    value = field.ValueOption == ValueOption.Count
                        ? selectable.SelectList(selector).Nodes().Count().ToString()
                        : selectable.Select(selector)?.GetValue(field.ValueOption);
                }

                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (field.Formatters != null && field.Formatters.Length > 0)
                    {
                        foreach (var formatter in field.Formatters)
                        {
#if DEBUG
                            try
                            {
#endif
                                value = formatter.Format(value);
#if DEBUG
                            }
                            catch (Exception e)
                            {
                                Debugger.Log(0, "ERROR", $"数据格式化失败: {e}");
                            }
#endif
                        }
                    }
                }

                var newValue = Convert.ChangeType(value, field.PropertyInfo.PropertyType);
                if (newValue == null && field.NotNull)
                {
                    dataObject = null;
                    break;
                }

                field.PropertyInfo.SetValue(dataObject, newValue);
            }

            return dataObject;
        }
    }
}