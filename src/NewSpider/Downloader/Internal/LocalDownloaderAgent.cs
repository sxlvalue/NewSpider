using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewSpider.Downloader.Entity;
using NewSpider.Infrastructure;
using NewSpider.MessageQueue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NewSpider.Downloader.Internal
{
    internal class LocalDownloaderAgent : AbstractDownloaderAgent
    {
        public LocalDownloaderAgent(IMessageQueue mq, ILoggerFactory loggerFactory) : base(Guid.NewGuid().ToString("N"),
            "LocalDownloaderAgent", mq, loggerFactory)
        {
        }

        protected override async Task<DownloaderEntry> CreateDownloaderEntry(
            AllotDownloaderMessage allotDownloaderMessage)
        {
            var downloadEntry = await base.CreateDownloaderEntry(allotDownloaderMessage);
            downloadEntry.Downloader.Logger = null;
            return downloadEntry;
        }
    }
}