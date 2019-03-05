using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NewSpider.Infrastructure;

namespace NewSpider.Downloader.Internal
{
    public class LocalDownloaderAgent : IDownloaderAgent
    {
        class HttpClientEntry
        {
            public HttpClient HttpClient { get; set; }
            public DateTime LastUsedTime { get; set; }
        }

        private readonly IMessageQueue _mq;

        private readonly ConcurrentDictionary<string, HttpClientEntry> _cache =
            new ConcurrentDictionary<string, HttpClientEntry>();

        public LocalDownloaderAgent(IMessageQueue mq)
        {
            _mq = mq;
        }

        public Task StartHeartbeatAsync()
        {
            // SendHeartbeart
            while (true)
            {
                Thread.Sleep(1000);
                LocalStore.Downloaders.TryAdd("Local-downloader", DateTime.Now);
                LocalStore.Downloaders["Local-downloader"] = DateTime.Now;
            }
        }


        public Task<List<object>> DownloadAsync(string ownerId, IEnumerable<IRequest> toObject)
        {
            return Task.FromResult(new List<object>() {"aaa"});
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public HttpClient GetHttpClientAsync(string ownerId)
        {
            HttpClientEntry output;
            if (_cache.TryGetValue(ownerId, out output))
            {
                return output.HttpClient;
            }
            else
            {
                throw new NewSpiderException("xx");
            }
        }

        public Task InitHttpClientAsync(string ownerId, string domain, string cookie, bool useProxy)
        {
            if (!_cache.ContainsKey(ownerId))
            {
                _cache.TryAdd(ownerId, new HttpClientEntry
                {
                    //TODO: cookie
                    HttpClient = new HttpClient()
                });
            }

            return Task.CompletedTask;
        }

        public Task CleanAsync()
        {
            return Task.CompletedTask;
        }
    }
}