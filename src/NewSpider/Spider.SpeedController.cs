using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NewSpider.Infrastructure;
using Newtonsoft.Json;

namespace NewSpider
{
    public partial class Spider
    {
        private Task StartSpeedControllerAsync()
        {
            _semaphore = Semaphore.Run;

            return Task.Factory.StartNew(async () =>
            {
                _logger.LogInformation($"任务 {Id} 速度控制器启动");
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

                                if (requests.Length > 0)
                                {
                                    await _downloadService.EnqueueRequests(Id, requests);
                                }
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
    }
}