using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autofac;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config.V2;
using BililiveRecorder.FlvProcessor;
using CommandLine;

namespace BililiveRecorder.Cli
{
    internal class Program
    {
        private static int Main()
            => Parser.Default
            .ParseArguments<CmdVerbConfigMode, CmdVerbPortableMode>(Environment.GetCommandLineArgs())
            .MapResult<CmdVerbConfigMode, CmdVerbPortableMode, int>(RunConfigMode, RunPortableMode, err => 1);

        private static int RunConfigMode(CmdVerbConfigMode opts)
        {
            var container = CreateBuilder().Build();
            var rootScope = container.BeginLifetimeScope("recorder_root");
            var semaphore = new SemaphoreSlim(0, 1);
            var recorder = rootScope.Resolve<IRecorder>();

            ConsoleCancelEventHandler p = null!;
            p = (sender, e) =>
            {
                Console.CancelKeyPress -= p;
                e.Cancel = true;
                recorder.Dispose();
                semaphore.Release();
            };
            Console.CancelKeyPress += p;

            if (!recorder.Initialize(opts.WorkDirectory))
            {
                Console.WriteLine("Initialize Error");
                return -1;
            }

            semaphore.Wait();
            return 0;
        }

        private static int RunPortableMode(CmdVerbPortableMode opts)
        {
            var container = CreateBuilder().Build();
            var rootScope = container.BeginLifetimeScope("recorder_root");
            var semaphore = new SemaphoreSlim(0, 1);
            var recorder = rootScope.Resolve<IRecorder>();
            var config = new ConfigV2()
            {
                DisableConfigSave = true,
            };

            if (!string.IsNullOrWhiteSpace(opts.Cookie))
                config.Global.Cookie = opts.Cookie;
            if (!string.IsNullOrWhiteSpace(opts.LiveApiHost))
                config.Global.LiveApiHost = opts.LiveApiHost;
            if (!string.IsNullOrWhiteSpace(opts.RecordFilenameFormat))
                config.Global.RecordFilenameFormat = opts.RecordFilenameFormat;

            config.Global.WorkDirectory = opts.OutputDirectory;
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

            if (!((Recorder)recorder).InitializeWithConfig(config))
            {
                Console.WriteLine("Initialize Error");
                return -1;
            }

            semaphore.Wait();
            return 0;
        }

        private static ContainerBuilder CreateBuilder()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<FlvProcessorModule>();
            builder.RegisterModule<CoreModule>();
            return builder;
        }
    }

    [Verb("portable", HelpText = "Run recorder. Ignore config file in output directory")]
    public class CmdVerbPortableMode
    {
        [Option('o', "dir", Default = ".", HelpText = "Output directory", Required = false)]
        public string OutputDirectory { get; set; } = ".";

        [Option("cookie", HelpText = "Provide custom cookies", Required = false)]
        public string? Cookie { get; set; }

        [Option("live_api_host", HelpText = "Use custom api host", Required = false)]
        public string? LiveApiHost { get; set; }

        [Option("record_filename_format", HelpText = "Recording name format", Required = false)]
        public string? RecordFilenameFormat { get; set; }

        [Value(0, Min = 1, Required = true, HelpText = "List of room id")]
        public IEnumerable<int> RoomIds { get; set; } = Enumerable.Empty<int>();
    }

    [Verb("run", HelpText = "Run recorder with config file")]
    public class CmdVerbConfigMode
    {
        [Value(0, HelpText = "Target directory", Required = true)]
        public string WorkDirectory { get; set; } = string.Empty;
    }
}
