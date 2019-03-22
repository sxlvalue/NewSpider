using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DotnetSpider.Core;
using DotnetSpider.Data.Parser;
using DotnetSpider.Data.Parser.Attribute;
using DotnetSpider.Data.Parser.Formatter;
using DotnetSpider.Data.Storage;
using DotnetSpider.Data.Storage.Model;
using DotnetSpider.Downloader;
using DotnetSpider.RequestSupply;
using DotnetSpider.Scheduler;
using DotnetSpider.Selector;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetSpider.Sample.samples
{
    public class EntitySpider : Spider
    {
        public static void Run()
        {
            var services = new ServiceCollection();
            services.AddDotnetSpider(builder =>
            {
                builder.UseConfiguration();
                builder.UseSerilog();
                builder.UseStandalone();
                builder.RegisterSpider(typeof(EntitySpider));
            });
            var factory = services.BuildServiceProvider().GetRequiredService<ISpiderFactory>();
            var spider = factory.Create<EntitySpider>();
            spider.RunAsync();
        }

        public EntitySpider(IServiceProvider services) : base(services)
        {
        }

        protected override void Initialize()
        {
            NewId();
            Scheduler = new QueueDistinctBfsScheduler();
            Speed = 1;
            Depth = 3;
            DownloaderType = DownloaderType.Default;
            AddDataFlow(new DataParser<BaiduSearchEntry>()).AddDataFlow(GetDefaultStorage());
            AddRequests(new Request
            {
                Url = "https://news.cnblogs.com/n/page/1/",
                Properties = new Dictionary<string, string> {{"网站", "博客园"}}
            }, new Request
            {
                Url = "https://news.cnblogs.com/n/page/2/",
                Properties = new Dictionary<string, string> {{"网站", "博客园"}}
            });
        }

        [Schema("cnblogs", "cnblogs_entity_model")]
        [EntitySelector(Expression = ".//div[@class='news_block']", Type = SelectorType.XPath)]
        [ValueSelector(Expression = ".//a[@class='current']", Name = "类别", Type = SelectorType.XPath)]
        class BaiduSearchEntry : EntityBase<BaiduSearchEntry>
        {
            protected override void Configure()
            {
                HasIndex(x => x.Title);
                HasIndex(x => new {x.WebSite, x.Guid}, true);
            }

            public int Id { get; set; }

            [Required]
            [StringLength(200)]
            [ValueSelector(Expression = "类别", Type = SelectorType.Enviroment)]
            public string Category { get; set; }

            [Required]
            [StringLength(200)]
            [ValueSelector(Expression = "网站", Type = SelectorType.Enviroment)]
            public string WebSite { get; set; }

            [StringLength(200)]
            [ValueSelector(Expression = "//title")]
            [ReplaceFormatter(NewValue = "", OldValue = " - 博客园")]
            public string Title { get; set; }

            [StringLength(40)]
            [ValueSelector(Expression = "GUID", Type = SelectorType.Enviroment)]
            public string Guid { get; set; }

            [ValueSelector(Expression = ".//h2[@class='news_entry']/a")]
            public string News { get; set; }

            [ValueSelector(Expression = ".//h2[@class='news_entry']/a/@href")]
            public string Url { get; set; }

            [ValueSelector(Expression = ".//div[@class='entry_summary']", ValueOption = ValueOption.InnerText)]
            public string PlainText { get; set; }

            [ValueSelector(Expression = "DATETIME", Type = SelectorType.Enviroment)]
            public DateTime CreationTime { get; set; }
        }
    }
}