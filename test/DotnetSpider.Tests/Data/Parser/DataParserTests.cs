using System.IO;
using DotnetSpider.Data;
using DotnetSpider.Data.Parser;
using DotnetSpider.Downloader;
using Xunit;

namespace DotnetSpider.Tests.Data.Parser
{
    public partial class DataParserTests : TestBase
    {
        /// <summary>
        /// 从 HTML 指定的 XPATH 元素下面查找所有的 URL
        /// </summary>
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

        /// <summary>
        /// 通过正则判断 URL 是否需要当前 DataParser 处理
        /// </summary>
        [Fact(DisplayName = "RegexCanParse")]
        public void RegexCanParse()
        {
            // TODO
        }
    }
}