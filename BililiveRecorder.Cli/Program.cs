using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V2;
using BililiveRecorder.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

namespace BililiveRecorder.Cli
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var cmd_run = new Command("run", "Run BililiveRecorder in standard mode")
            {
                new Argument<string>("path"),
            };
            cmd_run.Handler = CommandHandler.Create<string>(RunConfigMode);

            var cmd_portable = new Command("portable", "Run BililiveRecorder in config-less mode")
            {
                new Option<string>(new []{ "--cookie", "-c" }, "Cookie string for api requests"),
                new Option<string>("--live-api-host"),
                new Option<string>(new[]{ "--filename-format", "-f" }, "File name format"),
                new Argument<string>("output path"),
                new Argument<int[]>("room ids")
            };
            cmd_portable.Handler = CommandHandler.Create<PortableModeArguments>(RunPortableMode);

            var root = new RootCommand("A Stream Recorder For Bilibili Live")
            {
                cmd_run,
                cmd_portable
            };

            return root.Invoke(args);
        }

        private static int RunConfigMode(string path)
        {
            var logger = BuildLogger();
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
            throw new NotImplementedException();

#pragma warning disable CS0162 // Unreachable code detected
            var semaphore = new SemaphoreSlim(0, 1);
#pragma warning restore CS0162 // Unreachable code detected

            var serviceProvider = BuildServiceProvider(null, null);
            var recorder = serviceProvider.GetRequiredService<IRecorder>();

            var config = new ConfigV2()
            {
                DisableConfigSave = true,
            };

            if (!string.IsNullOrWhiteSpace(opts.Cookie))
                config.Global.Cookie = opts.Cookie;
            if (!string.IsNullOrWhiteSpace(opts.LiveApiHost))
                config.Global.LiveApiHost = opts.LiveApiHost;
            if (!string.IsNullOrWhiteSpace(opts.FilenameFormat))
                config.Global.RecordFilenameFormat = opts.FilenameFormat;

            config.Global.WorkDirectory = opts.OutputPath;
            config.Rooms = opts.RoomIds.Select(x => new RoomConfig { RoomId = x, AutoRecord = true }).ToList();

            ConsoleCancelEventHandler p = null!;
            p = (sender, e) =>
            {
                Console.CancelKeyPress -= p;
                e.Cancel = true;
                recorder.Dispose();
                semaphore.Release();
            };
            Console.CancelKeyPress += p;

            //if (!((DeadCodeRecorder)recorder).InitializeWithConfig(config))
            //{
            //    Console.WriteLine("Initialize Error");
            //    return -1;
            //}

            semaphore.Wait();
            return 0;
        }

        private static IServiceProvider BuildServiceProvider(ConfigV2 config, ILogger logger) => new ServiceCollection()
            .AddSingleton(logger)
            .AddFlv()
            .AddRecorderConfig(config)
            .AddRecorder()
            .BuildServiceProvider();

        private static ILogger BuildLogger() => new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Verbose, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{RoomId}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        public class PortableModeArguments
        {
            public string OutputPath { get; set; } = string.Empty;

            public string? Cookie { get; set; }

            public string? LiveApiHost { get; set; }

            public string? FilenameFormat { get; set; }

            public IEnumerable<int> RoomIds { get; set; } = Enumerable.Empty<int>();
        }
    }
}
