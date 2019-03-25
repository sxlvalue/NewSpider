using System;
using System.Threading.Tasks;
using DotnetSpider.Core;
using DotnetSpider.Downloader.Entity;
using DotnetSpider.MessageQueue;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Downloader.Internal
{
    internal class LocalDownloaderAgent : AbstractDownloaderAgent
    {
        public LocalDownloaderAgent(IMessageQueue mq, SpiderOptions options, IDownloaderAllocator downloaderAllocator,
            ILoggerFactory loggerFactory) : base(
            Guid.NewGuid().ToString("N"),
            "LocalDownloaderAgent", mq, options, downloaderAllocator, loggerFactory)
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