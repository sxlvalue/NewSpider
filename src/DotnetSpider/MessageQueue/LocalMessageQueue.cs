using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DotnetSpider.MessageQueue
{
    /// <summary>
    /// 1. 发布会把消息推送到所有订阅了对应 topic 的消费者
    /// 2. 只能对 topic 做取消订阅，会导致所有订阅都取消。 TODO: 是否需要考虑做指定取消定阅
    /// </summary>
    public class LocalMessageQueue : IMessageQueue
    {
        private readonly Dictionary<string, Action<string>> _consumers =
            new Dictionary<string, Action<string>>();

        public Task PublishAsync(string topic, params string[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                return Task.CompletedTask;
            }

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

            _consumers[topic] = action;
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