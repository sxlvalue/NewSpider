using System;
using DotnetSpider.Core;

namespace DotnetSpider.MessageQueue
{
    public static class MessageQueueExtensions
    {
        public static CommandMessage ToCommandMessage(this string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            var commandEndAt = message.IndexOf(DotnetSpiderConsts.CommandSeparator, StringComparison.Ordinal);
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