using System;
using System.Collections.Concurrent;

namespace NewSpider.Infrastructure
{
    public static class LocalStore
    {
        public static ConcurrentDictionary<string,DateTime> Downloaders=new ConcurrentDictionary<string, DateTime>();
    }
}