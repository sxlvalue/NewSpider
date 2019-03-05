using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewSpider.Downloader.Internal;
using Newtonsoft.Json;

namespace NewSpider.Downloader
{
    public class DownloaderAgentService : IDownloaderAgentService
    {
        private readonly IDownloaderAgent _agent;
        private readonly IMessageQueue _mq;

        public DownloaderAgentService(IMessageQueue mq)
        {
            _agent = new LocalDownloaderAgent(mq);
            _mq = mq;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _agent.StartHeartbeatAsync().ConfigureAwait(false);

            _mq.Subscribe("Local-downloader", async (message) =>
            {
                var @event = JsonConvert.DeserializeObject<DownloaderAgentEvent>(message);
                var ownerId = @event.Content["ownerId"].ToObject<string>();
                switch (@event.Event)
                {
                    case "init":
                    {
                        var domain = @event.Content["domain"].ToObject<string>();
                        var cookie = @event.Content["cookie"].ToObject<string>();
                        var useProxy = @event.Content["userProxy"].ToObject<bool>();
                        await _agent.InitHttpClientAsync(ownerId, domain, cookie, useProxy);
                        break;
                    }
                    case "download":
                    {
                        List<object> contents =
                            await _agent.DownloadAsync(ownerId, @event.Content.ToObject<IEnumerable<IRequest>>());
                        foreach (var content in contents)
                        {
                            await _mq.PublishAsync("Response-Handler-" + ownerId, content.ToString());
                        }


                        break;
                    }
                }
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}