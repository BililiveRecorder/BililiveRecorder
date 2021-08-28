using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using BililiveRecorder.Cli.Configure;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V2;
using BililiveRecorder.DependencyInjection;
using BililiveRecorder.ToolBox;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace BililiveRecorder.Cli
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var cmd_run = new Command("run", "Run BililiveRecorder in standard mode")
            {
                new Option<LogEventLevel>(new []{ "--loglevel", "--log", "-l" }, () => LogEventLevel.Information, "Minimal log level output to console"),
                new Option<LogEventLevel>(new []{ "--logfilelevel", "--flog" }, () => LogEventLevel.Debug, "Minimal log level output to file"),
                new Argument<string>("path"),
            };
            cmd_run.AddAlias("r");
            cmd_run.Handler = CommandHandler.Create<string, LogEventLevel, LogEventLevel>(RunConfigMode);

            var cmd_portable = new Command("portable", "Run BililiveRecorder in config-less mode")
            {
                new Option<LogEventLevel>(new []{ "--loglevel", "--log", "-l" }, () => LogEventLevel.Information, "Minimal log level output to console"),
                new Option<LogEventLevel>(new []{ "--logfilelevel", "--flog" }, () => LogEventLevel.Debug, "Minimal log level output to file"),
                new Option<string>(new []{ "--cookie", "-c" }, "Cookie string for api requests"),
                new Option<string>(new []{ "--filename-format", "-f" }, "File name format"),
                new Option<PortableModeArguments.PortableDanmakuMode>(new []{ "--danmaku", "-d" }, "Flags for danmaku recording"),
                new Option<string>("--webhook-url", "URL of webhoook"),
                new Option<string>("--live-api-host"),
                new Argument<string>("output-path"),
                new Argument<int[]>("room-ids")
            };
            cmd_portable.AddAlias("p");
            cmd_portable.Handler = CommandHandler.Create<PortableModeArguments>(RunPortableMode);

            var root = new RootCommand("A Stream Recorder For Bilibili Live")
            {
                cmd_run,
                cmd_portable,
                new ConfigureCommand(),
                new ToolCommand()
            };

            return root.Invoke(args);
        }

        private static int RunConfigMode(string path, LogEventLevel logLevel, LogEventLevel logFileLevel)
        {
            using var logger = BuildLogger(logLevel, logFileLevel);
            Log.Logger = logger;

            path = Path.GetFullPath(path);
            var config = ConfigParser.LoadFrom(path);
            if (config is null)
            {
                logger.Error("Initialize Error");
                return -1;
            }

            config.Global.WorkDirectory = path;

            var serviceProvider = BuildServiceProvider(config, logger);
            var recorder = serviceProvider.GetRequiredService<IRecorder>();

            ConsoleCancelEventHandler p = null!;
            var semaphore = new SemaphoreSlim(0, 1);
            p = (sender, e) =>
            {
                Console.CancelKeyPress -= p;
                e.Cancel = true;
                recorder.Dispose();
                semaphore.Release();
            };
            Console.CancelKeyPress += p;

            semaphore.Wait();
            Thread.Sleep(1000 * 2);
            Console.ReadLine();
            return 0;
        }

        private static int RunPortableMode(PortableModeArguments opts)
        {
            using var logger = BuildLogger(opts.LogLevel, opts.LogFileLevel);
            Log.Logger = logger;

            var config = new ConfigV2()
            {
                DisableConfigSave = true,
            };

            {
                var global = config.Global;

                if (!string.IsNullOrWhiteSpace(opts.Cookie))
                    global.Cookie = opts.Cookie;

                if (!string.IsNullOrWhiteSpace(opts.LiveApiHost))
                    global.LiveApiHost = opts.LiveApiHost;

                if (!string.IsNullOrWhiteSpace(opts.FilenameFormat))
                    global.RecordFilenameFormat = opts.FilenameFormat;

                if (!string.IsNullOrWhiteSpace(opts.WebhookUrl))
                    global.WebHookUrlsV2 = opts.WebhookUrl;

                var danmaku = opts.Danmaku;
                global.RecordDanmaku = danmaku != PortableModeArguments.PortableDanmakuMode.None;
                global.RecordDanmakuSuperChat = danmaku.HasFlag(PortableModeArguments.PortableDanmakuMode.SuperChat);
                global.RecordDanmakuGuard = danmaku.HasFlag(PortableModeArguments.PortableDanmakuMode.Guard);
                global.RecordDanmakuGift = danmaku.HasFlag(PortableModeArguments.PortableDanmakuMode.Gift);
                global.RecordDanmakuRaw = danmaku.HasFlag(PortableModeArguments.PortableDanmakuMode.RawData);

                global.WorkDirectory = opts.OutputPath;
                config.Rooms = opts.RoomIds.Select(x => new RoomConfig { RoomId = x, AutoRecord = true }).ToList();
            }

            var serviceProvider = BuildServiceProvider(config, logger);
            var recorder = serviceProvider.GetRequiredService<IRecorder>();

            ConsoleCancelEventHandler p = null!;
            var semaphore = new SemaphoreSlim(0, 1);
            p = (sender, e) =>
            {
                Console.CancelKeyPress -= p;
                e.Cancel = true;
                recorder.Dispose();
                semaphore.Release();
            };
            Console.CancelKeyPress += p;

            semaphore.Wait();
            Thread.Sleep(1000 * 2);
            Console.ReadLine();
            return 0;
        }

        private static IServiceProvider BuildServiceProvider(ConfigV2 config, ILogger logger) => new ServiceCollection()
            .AddSingleton(logger)
            .AddFlv()
            .AddRecorderConfig(config)
            .AddRecorder()
            .BuildServiceProvider();

        private static Logger BuildLogger(LogEventLevel logLevel, LogEventLevel logFileLevel) => new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Destructure.ByTransforming<Flv.Xml.XmlFlvFile.XmlFlvFileMeta>(x => new
            {
                x.Version,
                x.ExportTime,
                x.FileSize,
                x.FileCreationTime,
                x.FileModificationTime,
            })
            .WriteTo.Console(restrictedToMinimumLevel: logLevel, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{RoomId}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(new CompactJsonFormatter(), "./logs/bilirec.txt", restrictedToMinimumLevel: logFileLevel, shared: true, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
            .CreateLogger();

        public class PortableModeArguments
        {
            public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

            public LogEventLevel LogFileLevel { get; set; } = LogEventLevel.Debug;

            public string OutputPath { get; set; } = string.Empty;

            public string? Cookie { get; set; }

            public string? LiveApiHost { get; set; }

            public string? FilenameFormat { get; set; }

            public string? WebhookUrl { get; set; }

            public PortableDanmakuMode Danmaku { get; set; }

            public IEnumerable<int> RoomIds { get; set; } = Enumerable.Empty<int>();

            [Flags]
            public enum PortableDanmakuMode
            {
                None = 0,
                Danmaku = 1 << 0,
                SuperChat = 1 << 1,
                Guard = 1 << 2,
                Gift = 1 << 3,
                RawData = 1 << 4,
                All = Danmaku | SuperChat | Guard | Gift | RawData
            }
        }
    }
}
