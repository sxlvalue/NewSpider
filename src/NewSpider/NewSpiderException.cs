using System;

namespace NewSpider
{
    public class NewSpiderException : Exception
    {
        public NewSpiderException(string msg) : base(msg)
        {
        }
    }
}