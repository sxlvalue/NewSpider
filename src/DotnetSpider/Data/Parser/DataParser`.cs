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
        internal readonly Model<T> Model;
        internal readonly TableMetadata TableMetadata;

        public DataParser()
        {
            Model = new Model<T>();
            TableMetadata = new T().GetTableMetadata();
            var followXpaths = new HashSet<string>();
            foreach (var followSelector in Model.FollowSelectors)
            {
                foreach (var xPath in followSelector.XPaths)
                {
                    followXpaths.Add(xPath);
                }
            }

            var xpaths = followXpaths.ToArray();
            Follow = context => XpathFollow(xpaths).Invoke(context);
        }

        protected override Task<DataFlowResult> Parse(DataFlowContext context)
        {
            if (!context.Contains(Model.TypeName))
            {
                context.Add(Model.TypeName, TableMetadata);
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

            if (Model.ShareValueSelectors != null)
            {
                foreach (var selector in Model.ShareValueSelectors)
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

            bool singleExtractor = Model.Selector == null;

            if (!singleExtractor)
            {
                var selector = Model.Selector.ToSelector();

                var list = selectable.SelectList(selector).Nodes()?.ToList();
                if (list != null)
                {
                    if (Model.Take > 0 && list.Count > Model.Take)
                    {
                        list = Model.TakeFromHead
                            ? list.Take(Model.Take).ToList()
                            : list.Skip(list.Count - Model.Take).ToList();
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
                            Logger?.LogWarning($"解析到空数据，类型: {Model.TypeName}");
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
                    Logger?.LogWarning($"解析到空数据，类型: {Model.TypeName}");
                }
            }

            if (results.Count > 0)
            {
                var items = context.GetItem(Model.TypeName);
                if (items == null)
                {
                    context.AddItem(Model.TypeName, results);
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

            foreach (var field in Model.ValueSelectors)
            {
#if DEBUG
                if (field.PropertyInfo.Name == "Url")
                {
                }
#endif

                string value = null;
                if (field.Type == SelectorType.Enviroment)
                {
                    switch (field.Expression)
                    {
                        case "INDEX":
                        {
                            value = index.ToString();
                            break;
                        }
                        case "GUID":
                        {
                            value = Guid.NewGuid().ToString();
                            break;
                        }
                        case "DATE":
                        case "TODAY":
                        {
                            value = DateTime.Now.Date.ToString("yyyy-MM-dd");
                            break;
                        }
                        case "DATETIME":
                        case "NOW":
                        {
                            value = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                            break;
                        }
                        default:
                        {
                            if (environments.ContainsKey(field.Expression))
                            {
                                value = environments[field.Expression];
                            }

                            break;
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


                var newValue = value == null ? null : Convert.ChangeType(value, field.PropertyInfo.PropertyType);
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