using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewSpider.Downloader;
using NewSpider.Scheduler;
using Serilog;
using Serilog.Events;

namespace NewSpider
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console().WriteTo.RollingFile("new-spider.log")
                .CreateLogger();

            await new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<IDownloaderService>();
                    services.AddHostedService<ISchedulerService>();
                    services.AddLogging();
                }).ConfigureAppConfiguration(((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", true);
                    config.AddEnvironmentVariables();
                    if (args != null) config.AddCommandLine(args);
                }))
                .ConfigureLogging(((context,
                    builder) =>
                {
                    builder.AddSerilog();
                }))
                .RunConsoleAsync();
        }
    }
}