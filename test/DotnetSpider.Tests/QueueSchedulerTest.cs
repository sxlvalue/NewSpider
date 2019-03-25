using System;
using System.Threading.Tasks;
using DotnetSpider.Downloader;
using DotnetSpider.Scheduler;
using Xunit;

namespace DotnetSpider.Tests
{
    public class QueueSchedulerTests
    {
        [Fact(DisplayName = "QueueSchedulerPushAndPollAsync")]
        public void PushAndPollAsync()
        {
            var scheduler = new QueueDistinctBfsScheduler();
            var ownerId = Guid.NewGuid().ToString("N");
            Parallel.For(0, 1000, new ParallelOptions {MaxDegreeOfParallelism = 20}, i =>
            {
                scheduler.Enqueue(new[]
                {
                    new Request("http://www.a.com")
                    {
                        OwnerId = ownerId
                    }
                });
                scheduler.Enqueue(new[]
                {
                    new Request("http://www.a.com")
                    {
                        OwnerId = ownerId
                    }
                });
                scheduler.Enqueue(new[]
                {
                    new Request("http://www.a.com")
                    {
                        OwnerId = ownerId
                    }
                });
                scheduler.Enqueue(new[]
                {
                    new Request("http://www.b.com")
                    {
                        OwnerId = ownerId
                    }
                });
                scheduler.Enqueue(new[]
                {
                    new Request($"http://www.{i.ToString()}.com", null)
                    {
                        OwnerId = ownerId
                    }
                });
            });
            Parallel.For(0, 1000, new ParallelOptions {MaxDegreeOfParallelism = 20},
                i => { scheduler.Dequeue(ownerId); });

            Assert.Equal(2, scheduler.Requests[ownerId].Count);
            Assert.Equal(1002, scheduler.Total);
        }

        [Fact(DisplayName = "QueueScheduler_PushAndPollDepthFirst")]
        public void PushAndPollDepthFirst()
        {
            var ownerId = Guid.NewGuid().ToString("N");
            QueueDistinctDfsScheduler scheduler = new QueueDistinctDfsScheduler();
            scheduler.Enqueue(new[]
            {
                new Request("http://www.a.com")
                {
                    OwnerId = ownerId
                }
            });
            scheduler.Enqueue(new[]
            {
                new Request("http://www.a.com")
                {
                    OwnerId = ownerId
                }
            });
            scheduler.Enqueue(new[]
            {
                new Request("http://www.a.com")
                {
                    OwnerId = ownerId
                }
            });
            scheduler.Enqueue(new[]
            {
                new Request("http://www.b.com")
                {
                    OwnerId = ownerId
                }
            });

            var request = scheduler.Dequeue(ownerId)[0];
            Assert.Equal("http://www.b.com", request.Url);
            Assert.Equal(1, scheduler.Requests[ownerId].Count);
            Assert.Equal(2, scheduler.Total);
        }

        [Fact(DisplayName = "QueueScheduler_PushAndPollBreadthFirst")]
        public void PushAndPollBreadthFirst()
        {
            var ownerId = Guid.NewGuid().ToString("N");
            QueueDistinctBfsScheduler scheduler = new QueueDistinctBfsScheduler();
            scheduler.Enqueue(new[]
            {
                new Request("http://www.a.com")
                {
                    OwnerId = ownerId
                }
            });
            scheduler.Enqueue(new[]
            {
                new Request("http://www.b.com")
                {
                    OwnerId = ownerId
                }
            });
            scheduler.Enqueue(new[]
            {
                new Request("http://www.a.com")
                {
                    OwnerId = ownerId
                }
            });
            scheduler.Enqueue(new[]
            {
                new Request("http://www.a.com")
                {
                    OwnerId = ownerId
                }
            });

            var request = scheduler.Dequeue(ownerId)[0];
            Assert.Equal("http://www.a.com", request.Url);
            Assert.Equal(1, scheduler.Requests[ownerId].Count);
            Assert.Equal(2, scheduler.Total);
        }
    }
}