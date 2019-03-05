using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewSpider.Downloader;
using NewSpider.Downloader.Internal;
using NewSpider.Infrastructure;
using NewSpider.Pipeline;
using NewSpider.Processor;
using NewSpider.Scheduler;
using Newtonsoft.Json;

namespace NewSpider
{
    public partial class Spider : ISpider
    {
        private readonly IList<IPageProcessor> _processors = new List<IPageProcessor>();
        private readonly IList<IPipeline> _pipelines = new List<IPipeline>();
        private readonly IList<IDataFlow> _dataFlows = new List<IDataFlow>();
        private readonly IList<IRequest> _requests = new List<IRequest>();
        private readonly IDownloaderManager _downloaderService;
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;
        private readonly IMessageQueue _mq;
        private DateTime _lastRequestTime;
        private event RequestHandler BeforeDownload;
        private Semaphore _semaphore;
        private Status _status;
        private uint _emptySleepTime = 30;

        public uint Speed { get; set; } = 1;
        public uint DownloaderCount { get; set; } = 1;
        public string Id { get; }
        public string Name { get; }

        public uint EmptySleepTime
        {
            get => _emptySleepTime;
            set
            {
                if (value < 30)
                {
                    throw new NewSpiderException("EmptySleepTime should larger than 30");
                }

                _emptySleepTime = value;
            }
        }

        public Spider(string id, string name, IScheduler scheduler = null, IMessageQueue mq = null,
            IDownloaderManager downloaderService = null)
        {
            Id = id;
            Name = name;
            _scheduler = scheduler ?? new QueueScheduler();
            _mq = mq ?? new LocalMessageQueue();
            _downloaderService = downloaderService ?? new LocalDownloaderManager(_mq, new LocalDownloaderAgentStore());
            _logger = Log.CreateLogger(typeof(Spider).Name);
        }


        public async Task RunAsync()
        {
            try
            {
                _status = Status.Running;

                await RegisterDownloaderService();
                _logger.LogInformation("Register downloader service: OK");

                RunSpeedControllerAsync().ConfigureAwait(false);

                var dataFlows = new List<IDataFlow>();
                dataFlows.AddRange(_processors);
                dataFlows.AddRange(_dataFlows);
                dataFlows.AddRange(_pipelines);
                _mq.Subscribe("Response-Handler-" + Id, (message) =>
                {
                    _lastRequestTime = DateTime.Now;
                    var response = JsonConvert.DeserializeObject<ResponseEvent>(message);
                    var context = new FlowContext {Request = response.Request};

                    Parallel.ForEach(dataFlows, async df => { await df.Handle(context); });
                });
                _lastRequestTime = DateTime.Now;
                while (_semaphore != Semaphore.Exit)
                {
                    if ((DateTime.Now - _lastRequestTime).Seconds > EmptySleepTime)
                    {
                        Exit();
                    }

                    Thread.Sleep(1000);

                    // Report status
                }

                _logger.LogInformation("Spider exited");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            finally
            {
                _status = Status.Exited;
            }
        }

        private Task RunSpeedControllerAsync()
        {
            _semaphore = Semaphore.Run;

            return Task.Factory.StartNew(async () =>
            {
                _logger.LogInformation("Speed controller started");
                bool @break = false;
                while (!@break)
                {
                    Thread.Sleep(1000);

                    switch (_semaphore)
                    {
                        case Semaphore.Run:
                        {
                            try
                            {
                                var requests = (await _scheduler.PollAsync(Id, (int)Speed)).ToArray();
                                foreach (var request in requests)
                                {
                                    BeforeDownload?.Invoke(request);
                                }

                                await _downloaderService.PublishAsync(Id, requests);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e.ToString());
                            }

                            break;
                        }
                        case Semaphore.Pause:
                        {
                            break;
                        }
                        case Semaphore.Exit:
                        {
                            @break = true;
                            break;
                        }
                    }
                }
            });
        }

        private async Task RegisterDownloaderService()
        {
            await _downloaderService.RegisterAsync(Id, 1, 1);
        }

        public void Pause()
        {
            _semaphore = Semaphore.Pause;
        }

        public void Continue()
        {
            _semaphore = Semaphore.Run;
        }

        public void Exit()
        {
            _semaphore = Semaphore.Exit;
        }
    }
}