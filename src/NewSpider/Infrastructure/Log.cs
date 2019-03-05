using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace NewSpider.Infrastructure
{
    public static class Log
    {
        public static volatile ILoggerFactory Factory = new LoggerFactory();

        public static ILogger CreateLogger(string name)
        {
            return Factory.CreateLogger(name);
        }

        public static void UseConsole()
        {
            Factory.AddConsole();
        }

        public static void UseSerilog(LoggerConfiguration configuration = null)
        {
            if (configuration == null)
            {
                configuration = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console().WriteTo.RollingFile("new-spider.log");
            }

            Serilog.Log.Logger = configuration.CreateLogger();
            Factory.AddSerilog();
        }
    }
}