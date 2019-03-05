using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NewSpider
{
    public class LocalMessageQueue : IMessageQueue
    {
        private readonly ConcurrentDictionary<string, List<Action<string>>> _consumers =
            new ConcurrentDictionary<string, List<Action<string>>>();

        public Task PublishAsync(string topic, params string[] messages)
        {
            if (_consumers.ContainsKey(topic))
            {
                var consumers = _consumers[topic];
                foreach (var consumer in consumers)
                {
                    foreach (var message in messages)
                    {
                        Task.Factory.StartNew(() => { consumer.Invoke(message); }).ConfigureAwait(false);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public void Subscribe(string topic, Action<string> action)
        {
            _consumers.TryAdd(topic, new List<Action<string>>());
            _consumers[topic].Add(action);
        }
    }
}