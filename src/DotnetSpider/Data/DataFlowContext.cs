using System.Collections.Generic;
using System.Collections.Immutable;

namespace DotnetSpider.Data
{
    public class DataFlowContext
    {
        private readonly Dictionary<string, dynamic> _properties = new Dictionary<string, dynamic>();

        private readonly Dictionary<string, dynamic> _items = new Dictionary<string, dynamic>();

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

        public void AddItem(string name, dynamic value)
        {
            lock (this)
            {
                if (!_items.ContainsKey(name))
                {
                    _items.Add(name, value);
                }
                else
                {
                    _items[name] = value;
                }
            }
        }

        public IDictionary<string, dynamic> GetItems()
        {
            return _items.ToImmutableDictionary();
        }
    }
}