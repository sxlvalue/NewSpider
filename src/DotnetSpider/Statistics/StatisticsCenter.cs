using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetSpider.Core;
using DotnetSpider.Downloader;
using DotnetSpider.Downloader.Entity;
using DotnetSpider.MessageQueue;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DotnetSpider.Statistics
{
    public class StatisticsCenter : IStatisticsCenter
    {
        private bool _isRunning;

        private readonly IMessageQueue _mq;
        private readonly ILogger _logger;
        private readonly IStatisticsStore _statisticsStore;

        public StatisticsCenter(IMessageQueue mq, IStatisticsStore statisticsStore,
            ILoggerFactory loggerFactory)
        {
            _mq = mq;
            _statisticsStore = statisticsStore;
            _logger = loggerFactory.CreateLogger<StatisticsCenter>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
            {
                throw new DotnetSpiderException("统计中心正在运行中");
            }

            _logger.LogInformation("统计中心启动");

            _mq.Subscribe(DotnetSpiderConsts.StatisticsServiceTopic, async (message) =>
            {
                var commandMessage = message.ToCommandMessage();
                if (commandMessage == null)
                {
                    _logger.LogWarning($"接收到非法消息: {message}");
                    return;
                }

                switch (commandMessage.Command)
                {
                    case "Success":
                    {
                        var ownerId = commandMessage.Message;
                        await _statisticsStore.IncrementSuccessAsync(ownerId);
                        break;
                    }
                    case "Failed":
                    {
                        var ownerId = commandMessage.Message;
                        await _statisticsStore.IncrementFailedAsync(ownerId);
                        break;
                    }
                    case "Start":
                    {
                        var ownerId = commandMessage.Message;
                        await _statisticsStore.StartAsync(ownerId);
                        break;
                    }
                    case "Exit":
                    {
                        var ownerId = commandMessage.Message;
                        await _statisticsStore.ExitAsync(ownerId);
                        break;
                    }
                    case "Total":
                    {
                        var data = commandMessage.Message.Split(',');
                        await _statisticsStore.IncrementTotalAsync(data[0], int.Parse(data[1]));

                        break;
                    }
                    case "DownloadSuccess":
                    {
                        var data = commandMessage.Message.Split(',');
                        await _statisticsStore.IncrementDownloadSuccessAsync(data[0], int.Parse(data[1]),
                            long.Parse(data[2]));
                        break;
                    }
                    case "DownloadFailed":
                    {
                        var data = commandMessage.Message.Split(',');
                        await _statisticsStore.IncrementDownloadFailedAsync(data[0], int.Parse(data[1]));
                        break;
                    }
                    case "Print":
                    {
                        var ownerId = commandMessage.Message;
                        var statistics = await _statisticsStore.GetSpiderStatisticsAsync(ownerId);
                        _logger.LogTrace(
                            $"任务 {ownerId} 总计 {statistics.Total}, 成功 {statistics.Success}, 失败 {statistics.Failed}, 剩余 {(statistics.Total - statistics.Success - statistics.Failed)}");
                        break;
                    }
                }
            });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _mq.Unsubscribe(DotnetSpiderConsts.StatisticsServiceTopic);
            _isRunning = false;
            _logger.LogInformation("统计中心退出");
            return Task.CompletedTask;
        }
    }
}