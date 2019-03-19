using System.Collections.Generic;

namespace DotnetSpider.Data
{
    public class DataFlowContext
    {
        private readonly Dictionary<string, dynamic> _properties = new Dictionary<string, dynamic>();

        public readonly Dictionary<string, List<dynamic>> DataItems = new Dictionary<string, List<dynamic>>();

        public string Result { get; set; }

        public dynamic this[string key]
        {
            get
            {
                lock (this)
                {
                    return _properties.ContainsKey(key) ? _properties[key] : null;
                }
            }
            set
            {
                lock (this)
                {
                    if (_properties.ContainsKey(key))
                    {
                        _properties[key] = value;
                    }

                    else
                    {
                        _properties.Add(key, value);
                    }
                }
            }
        }

        public bool ContainsKey(string key)
        {
            lock (this)
            {
                return _properties.ContainsKey(key);
            }
        }

        public void AddDataItems(Dictionary<string, List<dynamic>> items)
        {
            
            if (items == null)
            {
                return;
            }

            lock (this)
            {
                foreach (var item in items)
                {
                    if (!DataItems.ContainsKey(item.Key))
                    {
                        DataItems.Add(item.Key, item.Value);
                    }
                    else
                    {
                        DataItems[item.Key].AddRange(item.Value);
                    }
                }
            }
        }
    }
}