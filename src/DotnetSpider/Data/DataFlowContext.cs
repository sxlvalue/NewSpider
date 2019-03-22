using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DotnetSpider.Data
{
    public class DataFlowContext
    {
        private readonly Dictionary<string, dynamic> _properties = new Dictionary<string, dynamic>();

        private readonly Dictionary<string, dynamic> _items = new Dictionary<string, dynamic>();

        public IServiceProvider Services { get;  }      

        public DataFlowContext(IServiceProvider serviceProvider)
        {
            Services = serviceProvider;
        }

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
                    _properties.Add(key, value);
                }
            }
        }

        public bool Contains(string key)
        {
            return _properties.ContainsKey(key);
        }

        public void Add(string key, dynamic value)
        {
            _properties.Add(key, value);
        }

        public void AddItem(string name, dynamic value)
        {
            if (_items.ContainsKey(name))
            {
                _items[name] = value;
            }

            else
            {
                _items.Add(name, value);
            }
        }

        public dynamic GetItem(string name)
        {
            return _items.ContainsKey(name) ? _items[name] : null;
        }

        public IDictionary<string, dynamic> GetItems()
        {
            return _items.ToImmutableDictionary();
        }
    }
}