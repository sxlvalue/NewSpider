using System;
using System.Threading.Tasks;
using DotnetSpider.Core;
using DotnetSpider.Downloader;
using Xunit;

namespace DotnetSpider.Tests
{
    public class HashSetDuplicateRemoverTest
    {
        [Fact(DisplayName = "HashSetDuplicate")]
        public void HashSetDuplicate()
        {
            Scheduler.Component.HashSetDuplicateRemover scheduler = new Scheduler.Component.HashSetDuplicateRemover();

            var ownerId = Guid.NewGuid().ToString("N");
            var r1 = new Request("http://www.a.com")
            {
                OwnerId = ownerId
            };
            r1.ComputeHash();
            bool isDuplicate = scheduler.IsDuplicate(r1);

            Assert.False(isDuplicate);
            var r2 = new Request("http://www.a.com")
            {
                OwnerId = ownerId
            };
            r2.ComputeHash();
            isDuplicate = scheduler.IsDuplicate(r2);
            Assert.True(isDuplicate);
            var r3 = new Request("http://www.b.com")
            {
                OwnerId = ownerId
            };
            r3.ComputeHash();
            isDuplicate = scheduler.IsDuplicate(r3);
            Assert.False(isDuplicate);
            var r4 = new Request("http://www.b.com")
            {
                OwnerId = ownerId
            };
            r4.ComputeHash();

            isDuplicate = scheduler.IsDuplicate(r4);
            Assert.True(isDuplicate);
        }

        [Fact(DisplayName = "ParallelHashSetDuplicate")]
        public void ParallelHashSetDuplicate()
        {
            var ownerId = Guid.NewGuid().ToString("N");
            Scheduler.Component.HashSetDuplicateRemover scheduler = new Scheduler.Component.HashSetDuplicateRemover();
            var r1 = new Request("http://www.a.com")
            {
                OwnerId = ownerId
            };
            r1.ComputeHash();
            bool isDuplicate = scheduler.IsDuplicate(r1);

            Assert.False(isDuplicate);
            Parallel.For(0, 1000, new ParallelOptions {MaxDegreeOfParallelism = 30}, i =>
            {
                var r = new Request("http://www.a.com")
                {
                    OwnerId = ownerId
                };
                r.ComputeHash();
                isDuplicate = scheduler.IsDuplicate(r);
                Assert.True(isDuplicate);
            });
        }

        protected virtual void ComputeHash(Request request)
        {
            var content = $"{request.OwnerId}{request.Url}{request.Method}{request.Body}";
            request.Hash = content.ToMd5();
        }
    }
}