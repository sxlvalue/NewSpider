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
using NewSpider.Downloader.Entity;
using NewSpider.Downloader.Internal;
using NewSpider.Infrastructure;
using NewSpider.MessageQueue;
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

        private readonly IMessageQueue _mq;
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;
        private DateTime _lastRequestTime;
        private event RequestHandler BeforeDownload;
        private Semaphore _semaphore;
        private Status _status;
        private uint _emptySleepTime = 30;

        public uint Speed { get; set; } = 1;
        public uint DownloaderCount { get; set; } = 1;
        public string Id { get; }
        public string Name { get; }

        public bool IsDistributed { get; set; }

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
            IDownloaderManager dm = null)
        {
            Id = id;
            Name = name;
            _scheduler = scheduler ?? new QueueScheduler();
            IsDistributed = mq != null;
            _mq = mq ?? new LocalMessageQueue();

            _logger = Log.CreateLogger(typeof(Spider).Name);
        }


        public async Task RunAsync()
        {
            try
            {
                _status = Status.Running;

                if (!IsDistributed)
                {
                    var agent = new LocalDownloaderAgent(_mq);
                    agent.StartAsync(new CancellationToken()).ConfigureAwait(false);
                }

                await AllotDownloaderAsync();
                _logger.LogInformation("Register downloader service: OK");

                RunSpeedControllerAsync().ConfigureAwait(false);

                var dataFlows = new List<IDataFlow>();
                dataFlows.AddRange(_processors);
                dataFlows.AddRange(_dataFlows);
                dataFlows.AddRange(_pipelines);
                _mq.Subscribe($"{NewSpiderConsts.ResponseHandlerTopic}{Id}", (message) =>
                {
                    _lastRequestTime = DateTime.Now;
                    var responses = JsonConvert.DeserializeObject<List<Response>>(message);
                    Parallel.ForEach(responses, (response) =>
                    {
                        var context = new FlowContext {Request = response.Request};

                        Parallel.ForEach(dataFlows, async df => { await df.Handle(context); });

                        _logger.LogInformation($"Handle {response.Request.Url} success");
                    });
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
                                var requests = (await _scheduler.PollAsync(Id, (int) Speed)).ToArray();
                                foreach (var request in requests)
                                {
                                    BeforeDownload?.Invoke(request);
                                }

                                var json = JsonConvert.SerializeObject(requests);
                                await _mq.PublishAsync(NewSpiderConsts.DownloaderCenterTopic, $"Download|{json}");
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

        private async Task AllotDownloaderAsync()
        {
            var json = JsonConvert.SerializeObject(new AllotDownloaderMessage
            {
                OwnerId = Id,
                Type = DownloaderType.Empty,
                Speed = Speed,
                UseProxy = false
            });
            await _mq.PublishAsync(NewSpiderConsts.DownloaderCenterTopic, $"Allocate|{json}");
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