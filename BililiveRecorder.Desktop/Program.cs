using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.ToolBox;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

#nullable enable
namespace BililiveRecorder.Desktop
{
    internal static class Program
    {
        private const int CODE__AVALONIA = 0x5F_41_56_41;

        internal static readonly LoggingLevelSwitch levelSwitchGlobal;
        internal static readonly LoggingLevelSwitch levelSwitchConsole;
        internal static readonly Logger logger;

#if DEBUG
        internal static readonly bool DebugMode = System.Diagnostics.Debugger.IsAttached;
#else
        internal static readonly bool DebugMode = false;
#endif

        static Program()
        {
            levelSwitchGlobal = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Debug);
            if (DebugMode)
                levelSwitchGlobal.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
            levelSwitchConsole = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Error);
            logger = BuildLogger();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            Log.Logger = logger;
            ServicePointManager.Expect100Continue = false;
        }

        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                logger.Debug("Starting, Version: {Version}, CurrentDirectory: {CurrentDirectory}, CommandLine: {CommandLine}",
                             GitVersionInformation.InformationalVersion,
                             Environment.CurrentDirectory,
                             Environment.CommandLine);
                var code = BuildCommand().Invoke(args);
                logger.Debug("Exit code: {ExitCode}, RunAvalonia: {RunAvalonia}", code, code == CODE__AVALONIA);
                return code == CODE__AVALONIA ? Commands.RunAvaloniaReal(args) : code;
            }
            finally
            {
                logger.Dispose();
            }
        }

        private static RootCommand BuildCommand()
        {
            var run = new Command("run", "Run BililiveRecorder at path")
            {
                new Argument<string?>("path", () => null, "Work directory"),
                new Option<bool>("--ask-path", "Ask path in GUI even when \"don't ask again\" is selected before."),
                new Option<bool>("--hide", "Minimize to tray")
            };
            run.Handler = CommandHandler.Create((string? path, bool askPath, bool hide) => Commands.RunAvaloniaHandler(path: path, askPath: askPath, hide: hide));

            var root = new RootCommand("")
            {
                run,
                new ToolCommand(),
            };
            root.Handler = CommandHandler.Create(() => Commands.RunAvaloniaHandler(path: null, askPath: false, hide: false));
            return root;
        }

        private static class Commands
        {
            internal static int RunAvaloniaHandler(string? path, bool askPath, bool hide)
            {
                Pages.RootPage.CommandArgumentRecorderPath = path;
                Pages.RootPage.CommandArgumentAskPath = askPath;
                Pages.RootPage.CommandArgumentHide = hide;
                return CODE__AVALONIA;
            }

            internal static int RunAvaloniaReal(string[] args)
            {
                try
                {
                    return BuildAvaloniaApp()
                        .StartWithClassicDesktopLifetime(args);
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, "Fatal error in Avalonia application");
                    return 1;
                }
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static Logger BuildLogger()
        {
            var logFilePath = Environment.GetEnvironmentVariable("BILILIVERECORDER_LOG_FILE_PATH");
            if (string.IsNullOrWhiteSpace(logFilePath))
                logFilePath = Path.Combine(AppContext.BaseDirectory, "logs", "bilirec.txt");

            return new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitchGlobal)
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.WithExceptionDetails()
                .Destructure.AsScalar<IPAddress>()
                .Destructure.AsScalar<ProcessingComment>()
                .Destructure.AsScalar<StreamCodecQn>()
                .Destructure.ByTransforming<Flv.Xml.XmlFlvFile.XmlFlvFileMeta>(x => new
                {
                    x.Version,
                    x.ExportTime,
                    x.FileSize,
                    x.FileCreationTime,
                    x.FileModificationTime,
                })
                .WriteTo.Console(levelSwitch: levelSwitchConsole)
#if DEBUG
                .WriteTo.Debug()
#endif
                .WriteTo.Async(l => l.File(new CompactJsonFormatter(), logFilePath, shared: true, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true))
                .CreateLogger();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                logger.Fatal(ex, "Unhandled exception from AppDomain.UnhandledException");
        }

        private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e) =>
            logger.Error(e.Exception, "Unobserved exception from TaskScheduler.UnobservedTaskException");
    }
}
