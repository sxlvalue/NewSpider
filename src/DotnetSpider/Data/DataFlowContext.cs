using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DotnetSpider.Data
{
    public class DataFlowContext
    {
        private readonly ConcurrentDictionary<string, dynamic> _properties = new ConcurrentDictionary<string, dynamic>();

        private readonly ConcurrentDictionary<string, dynamic> _items = new ConcurrentDictionary<string, dynamic>();

        public string Result { get; set; }

        public dynamic this[string key]
        {
            get => _properties.ContainsKey(key) ? _properties[key] : null;
            set
            {
                if (_properties.ContainsKey(key))
                {
                    _properties[key] = value;
                }

                else
                {
                    _properties.TryAdd(key, value);
                }
            }
        }

        public bool Contains(string key)
        {
            return _properties.ContainsKey(key);
        }

        public void AddItem(string name, dynamic value)
        {
            if (_items.ContainsKey(name))
            {
                _items[name] = value;
            }

            else
            {
                _items.TryAdd(name, value);
            }
        }

        public IDictionary<string, dynamic> GetItems()
        {
            return _items.ToImmutableDictionary();
        }
    }
}