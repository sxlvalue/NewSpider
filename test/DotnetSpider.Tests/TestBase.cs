using System;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetSpider.Tests
{
    public class TestBase
    {
        protected readonly ISpiderFactory SpiderFactory;

        protected TestBase()
        {
            var services = new ServiceCollection();
            services.AddDotnetSpider(builder =>
            {
                builder.UseConfiguration();
                builder.UseSerilog();
                builder.UseStandalone();
            });
            SpiderFactory = services.BuildServiceProvider().GetRequiredService<ISpiderFactory>();
        }
    }
}