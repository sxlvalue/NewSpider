using System;
using System.Threading.Tasks;

namespace NewSpider.MessageQueue
{
    public interface IMessageQueue
    {
        Task PublishAsync(string topic, params string[] messages);

        void Subscribe(string topic, Action<string> action);

        void Unsubscribe(string topic);
    }
}