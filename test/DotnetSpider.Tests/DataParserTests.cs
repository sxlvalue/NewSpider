using System.IO;
using DotnetSpider.Data;
using DotnetSpider.Data.Parser;
using DotnetSpider.Downloader;
using Xunit;

namespace DotnetSpider.Tests
{
    public partial class DataParserTests : TestBase
    {
        [Fact(DisplayName = "XpathFollow")]
        public void XpathFollow()
        {
            var service = SpiderFactory.CreateScope();
            var dataContext = new DataFlowContext(service);
            dataContext.AddResponse(new Response
            {
                Request = new Request("http://cnblogs.com"),
                RawText = File.ReadAllText("cnblogs.html")
            });
            var xpathFollow = DataParser.XpathFollow(".//div[@class='pager']");

            var requests = xpathFollow.Invoke(dataContext);
 
            Assert.Equal(12, requests.Length);
            Assert.Contains(requests, r => r == "http://cnblogs.com/sitehome/p/2");
        }
    }
}