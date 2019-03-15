using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NewSpider.MessageQueue
{
    public class LocalMessageQueue : IMessageQueue
    {
        private readonly Dictionary<string, Action<string>> _consumers =
            new Dictionary<string, Action<string>>();

        // private readonly ConcurrentDictionary<string,List<string>> _messageQueue = new ConcurrentDictionary<string, List<string>>();

        public Task PublishAsync(string topic, params string[] messages)
        {
            if (_consumers.ContainsKey(topic))
            {
                var consumer = _consumers[topic];
                foreach (var message in messages)
                {
                    Task.Factory.StartNew(() => { consumer.Invoke(message); }).ConfigureAwait(false);
                }
            }

            return Task.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Subscribe(string topic, Action<string> action)
        {
            if (!_consumers.ContainsKey(topic))
            {
                _consumers.Add(topic, action);
            }

            _consumers[topic]=action;            
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Unsubscribe(string topic)
        {
            if (_consumers.ContainsKey(topic))
            {
                _consumers.Remove(topic);
            }
        }
    }
}