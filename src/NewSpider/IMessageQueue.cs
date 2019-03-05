using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewSpider
{
    public interface IMessageQueue
    {
        Task PublishAsync(string topic, params string[] messages);

        void Subscribe(string topic, Action<string> action);
    }
}