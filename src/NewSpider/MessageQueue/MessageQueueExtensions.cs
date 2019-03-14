using System;
using NewSpider.Infrastructure;

namespace NewSpider.MessageQueue
{
    public static class MessageQueueExtensions
    {
        public static CommandMessage ToCommandMessage(this string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            var commandEndAt = message.IndexOf(NewSpiderConsts.CommandSeparator, StringComparison.Ordinal);
            return commandEndAt > 0
                ? new CommandMessage
                {
                    Command = message.Substring(0, commandEndAt),
                    Message = message.Substring(commandEndAt + 1)
                }
                : null;
        }
    }
}