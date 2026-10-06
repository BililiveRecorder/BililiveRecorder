using System;
using System.IO;
using System.Threading;
using Avalonia;
using BililiveRecorder.Desktop.Infrastructure;
using Serilog;
using Serilog.Core;
using Serilog.Events;

#nullable enable
namespace BililiveRecorder.Desktop
{
    internal static class Program
    {
        private static readonly Logger logger;

        /// <summary>命令行传入的工作目录参数（可能为 null，路径合法性由 App 启动流程校验）。</summary>
        internal static string? PathArgument { get; private set; }

        /// <summary>忽略记住的路径，强制显示目录选择窗口（--ask-path）。</summary>
        internal static bool AskPath { get; private set; }

        /// <summary>是否以最小化到托盘的方式启动（--hide）。</summary>
        internal static bool StartMinimized { get; private set; }

        static Program()
        {
            logger = BuildLogger();
            Log.Logger = logger;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            logger.Debug("Starting, CurrentDirectory: {CurrentDirectory}, CommandLine: {CommandLine}",
                         Environment.CurrentDirectory, Environment.CommandLine);

            ParseArguments(args);
            SleepBlocker.Start();

            // 工作目录的解析与校验在 App.OnFrameworkInitializationCompleted 中进行：
            // 能自动确定就直接进入主界面，否则先启动软件再显示目录选择窗口（与 WPF 版一致）
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        private static void ParseArguments(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--hide":
                        StartMinimized = true;
                        break;
                    case "--ask-path":
                        AskPath = true;
                        break;
                    case "-h" or "--help":
                        Console.WriteLine(@"用法 Usage:
  BililiveRecorder.Desktop [run] [路径 path] [--hide] [--ask-path]");
                        Environment.Exit(0);
                        break;
                    case "run":
                        break;
                    default:
                        if (!args[i].StartsWith('-') && PathArgument is null)
                            PathArgument = args[i];
                        break;
                }
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        private static Logger BuildLogger()
        {
            var logFilePath = Environment.GetEnvironmentVariable("BILILIVERECORDER_LOG_FILE_PATH");
            if (string.IsNullOrWhiteSpace(logFilePath))
                logFilePath = Path.Combine(AppContext.BaseDirectory, "logs", "bilirec.txt");

            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.Async(l => l.Sink(new MemoryLogSink(), LogEventLevel.Debug))
                .WriteTo.Async(l => l.File(logFilePath, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, shared: true))
                .CreateLogger();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                logger.Fatal(ex, "Unhandled exception from AppDomain.UnhandledException");
        }
    }
}
