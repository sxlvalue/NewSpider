using System;

namespace DotnetSpider.Core
{
    public class DotnetSpiderException : Exception
    {
        public DotnetSpiderException(string msg) : base(msg)
        {
        }
    }
}