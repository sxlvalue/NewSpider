using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using CommandLine;
using Microsoft.Extensions.Configuration;

namespace DotnetSpider.Core
{
    /// <summary>
    /// 启动任务工具
    /// </summary>
    public static class Startup
    {
        public static readonly Dictionary<string, string> _switchMappings =
            new Dictionary<string, string>
            {
                {"-s", "spider"},
                {"-n", "name"},
                {"-i", "id"},
                {"-c", "config"},
                {"-a", "args"},
            };

        /// <summary>
        /// DLL 名字中包含任意一个即是需要扫描的 DLL
        /// </summary>
        public static readonly List<string> DetectNames = new List<string> {"dotnetspider.sample"};

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="args">运行参数</param>
        public static void Run(params string[] args)
        {
            args = new[] {"-s", "atest"};
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddCommandLine(args, _switchMappings);
            configurationBuilder.AddEnvironmentVariables();

            var configuration = configurationBuilder.Build();
            var spider = configuration["spider"];
            var name = configuration["name"];
            var id = configuration["id"];
            var config = configuration["config"];
            var arguments = configuration["args"];

//            PrintEnvironment(args);
//
//            var builder = new LocalSpiderBuilder();
//            builder.UseSerilog(); // 可以配置任意日志组件
//            builder.UseDistinctScheduler(); // 配置本地内存调度或者数据库调度
//            builder.UseConfiguration(options.Config);
//
//
//            var spiderName = options.Spider;
//
//            var spiderTypes = DetectSpiders();
//
//            if (spiderTypes == null || spiderTypes.Count == 0)
//            {
//                return;
//            }
//
//            var spider = CreateSpiderInstance(spiderName, options, spiderTypes);
//            if (spider != null)
//            {
//                Framework.PrintLine();
//
//                var runMethod = spiderTypes[spiderName].GetMethod("Run");
//
//                if (runMethod != null) runMethod.Invoke(spider, new object[] {options.GetArguments()});
//            }
        }

        /// <summary>
        /// 检测爬虫类型
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, Type> DetectSpiders()
        {
            var spiderTypes = new Dictionary<string, Type>();

//            var spiderType = typeof(Spider);
//            foreach (var file in DetectAssemblies())
//            {
//                var asm = Assembly.Load(file);
//                var types = asm.GetTypes();
//
//                Console.WriteLine($"Fetch assembly   : {asm.GetName(false)}.");
//
//                foreach (var type in types)
//                {
//                    if (hasNonParametersConstructor)
//                    {
//                        if (isNamed && isRunnable && isIdentity)
//                        {
//                            if (!spiderTypes.ContainsKey(fullName))
//                            {
//                                spiderTypes.Add(fullName, type);
//                            }
//                            else
//                            {
//                                ConsoleHelper.WriteLine($"Spider {type.Name} are duplicate.", 1);
//                                return null;
//                            }
//
//                            var startupName =
//                                type.GetCustomAttributes(typeof(TaskName), true).FirstOrDefault() as TaskName;
//                            if (startupName != null)
//                            {
//                                if (!spiderTypes.ContainsKey(startupName.Name))
//                                {
//                                    spiderTypes.Add(startupName.Name, type);
//                                }
//                                else
//                                {
//                                    ConsoleHelper.WriteLine($"Spider {type.Name} are duplicate.", 1);
//                                    return null;
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//
//            if (spiderTypes.Count == 0)
//            {
//                ConsoleHelper.WriteLine("Did not detect any spider.", 1, ConsoleColor.DarkYellow);
//                return null;
//            }
//
//            Console.WriteLine($"Count of crawlers: {spiderTypes.Keys.Count}");

            return spiderTypes;
        }

        /// <summary>
        /// 扫描所有需要求的DLL
        /// </summary>
        /// <returns></returns>
        public static List<string> DetectAssemblies()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
            var files = Directory.GetFiles(path)
                .Where(f => f.EndsWith(".dll") || f.EndsWith(".exe"))
                .Select(f => Path.GetFileName(f).Replace(".dll", "").Replace(".exe", "")).ToList();
            return
                files.Where(f => !f.EndsWith("DotnetSpider")
                                 && DetectNames.Any(n => f.ToLower().Contains(n))).ToList();
        }

//        /// <summary>
//        /// 反射爬虫对象
//        /// </summary>
//        /// <param name="spiderName">名称</param>
//        /// <param name="options">运行参数</param>
//        /// <param name="spiderTypes">所有的爬虫类型</param>
//        /// <returns>爬虫对象</returns>
//        public static object CreateSpiderInstance(string spiderName,  
//            Dictionary<string, Type> spiderTypes)
//        {
//            if (!spiderTypes.ContainsKey(spiderName))
//            {
//                ConsoleHelper.WriteLine($"Spider: {spiderName} unfounded", ConsoleColor.DarkYellow);
//                return null;
//            }
//
//            var spiderType = spiderTypes[spiderName];
//
//            var spider = Activator.CreateInstance(spiderType);
//            var spiderProperties = spiderType.GetProperties();
//
//            if (!string.IsNullOrWhiteSpace(options.Identity))
//            {
//                var identity = "guid" == options.Identity.ToLower()
//                    ? Guid.NewGuid().ToString("N")
//                    : options.Identity.Trim();
//                if (!string.IsNullOrWhiteSpace(identity))
//                {
//                    var property = spiderProperties.First(p => p.Name == "Identity");
//                    property.SetValue(spider, identity, new object[0]);
//                }
//            }
//
//            if (!string.IsNullOrWhiteSpace(options.TaskId))
//            {
//                var property = spiderProperties.FirstOrDefault(p => p.Name == "TaskId");
//                if (property != null)
//                {
//                    var taskId = "guid" == options.TaskId.ToLower()
//                        ? Guid.NewGuid().ToString("N")
//                        : options.TaskId.Trim();
//                    if (!string.IsNullOrWhiteSpace(taskId))
//                    {
//                        property.SetValue(spider, taskId, new object[0]);
//                    }
//                }
//            }
//
//            if (!string.IsNullOrWhiteSpace(options.Name))
//            {
//                var property = spiderProperties.First(p => p.Name == "Name");
//                property.SetValue(spider, options.Name.Trim(), new object[0]);
//            }
//
//            return spider;
//        }

        private static void PrintEnvironment(params string[] args)
        {
            Console.WriteLine("");
            Framework.PrintInfo();
            var commands = string.Join(" ", args);
            Framework.PrintLine();
            Console.WriteLine($"Args             : {commands}");
            Console.WriteLine($"BaseDirectory    : {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine(
                $"System           : {Environment.OSVersion} {(Environment.Is64BitOperatingSystem ? "X64" : "X86")}");
        }

        private static void SetEncoding()
        {
#if NETSTANDARD
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }
    }
}