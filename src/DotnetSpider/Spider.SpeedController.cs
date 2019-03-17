using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotnetSpider
{
    public partial class Spider
    {
        private Task StartSpeedControllerAsync()
        {
            return Task.Factory.StartNew(async () =>
            {
                _logger.LogInformation($"任务 {Id} 速度控制器启动");
                bool @break = false;


                while (!@break)
                {
                    Thread.Sleep(_speedControllerInterval);

                    switch (_status)
                    {
                        case   Status.Running :
                        {
                            try
                            {
                                var requests =  _scheduler.Dequeue(Id, _dequeueBatchCount);
                                foreach (var request in requests)
                                {
                                    PretreatmentDownload?.Invoke(request);
                                }

                                if (requests.Length > 0)
                                {
                                    await _downloadService.EnqueueRequests(Id, requests);
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError($"速度控制器运转失败: {e}");
                            }

                            break;
                        }
                        case Status.Paused:
                        {
                            _logger.LogInformation($"任务 {Id} 速度控制器暂停");
                            break;
                        }
                        case Status.Exited:
                        {
                            @break = true;
                            break;
                        }
                    }
                }

                _logger.LogInformation($"任务 {Id} 速度控制器退出");
            });
        }
    }
}