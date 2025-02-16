using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.ToolBox;
using Esprima;
using Jint.Runtime;
using Sentry;
using Sentry.Extensibility;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;
using Squirrel;

#nullable enable
namespace BililiveRecorder.WPF
{
    internal static class Program
    {
        private const int CODE__WPF = 0x5F_57_50_46;

        internal static readonly LoggingLevelSwitch levelSwitchGlobal;
        internal static readonly LoggingLevelSwitch levelSwitchConsole;
        internal static readonly Logger logger;
        internal static readonly Update update;
        internal static Task? updateTask;

#if DEBUG
        internal static readonly bool DebugMode = System.Diagnostics.Debugger.IsAttached;
#else
        internal static readonly bool DebugMode = false;
#endif

        static Program()
        {
            AttachConsole(-1);
            levelSwitchGlobal = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Debug);
            if (DebugMode)
                levelSwitchGlobal.MinimumLevel = Serilog.Events.LogEventLevel.Verbose;
            levelSwitchConsole = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Error);
            logger = BuildLogger();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            Log.Logger = logger;
            SentrySdk.ConfigureScope(s =>
            {
                s.SetTag("fullsemver", GitVersionInformation.FullSemVer);
            });
            _ = SentrySdk.ConfigureScopeAsync(async s =>
            {
                var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "..", "packages", ".betaId"));
                for (var i = 0; i < 10; i++)
                {
                    if (i != 0)
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    try
                    {
                        if (!File.Exists(path))
                            continue;
                        var content = File.ReadAllText(path);
                        if (Guid.TryParse(content, out var id))
                        {
                            s.User.Id = id.ToString();
                            return;
                        }
                    }
                    catch (Exception)
                    { }
                }
            });
            ServicePointManager.Expect100Continue = false;
            update = new Update(logger);
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
                logger.Debug("Exit code: {ExitCode}, RunWpf: {RunWpf}", code, code == CODE__WPF);
                return code == CODE__WPF ? Commands.RunWpfReal() : code;
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
            run.Handler = CommandHandler.Create((string? path, bool askPath, bool hide) => Commands.RunWpfHandler(path: path, squirrelFirstrun: false, askPath: askPath, hide: hide));

            var root = new RootCommand("")
            {
                new Option<bool>("--squirrel-firstrun")
                {
                    IsHidden = true
                },
                new Option<SemanticVersion?>("--squirrel-install", getDefaultValue: () => null)
                {
                    IsHidden = true,
                    IsRequired = false,
                },
                new Option<SemanticVersion?>("--squirrel-updated", getDefaultValue: () => null)
                {
                    IsHidden = true,
                    IsRequired = false,
                },
                new Option<SemanticVersion?>("--squirrel-obsolete", getDefaultValue: () => null)
                {
                    IsHidden = true,
                    IsRequired = false,
                },
                new Option<SemanticVersion?>("--squirrel-uninstall", getDefaultValue: () => null)
                {
                    IsHidden = true,
                    IsRequired = false,
                },

                run,
                new ToolCommand(),
            };
            root.Handler = CommandHandler.Create<bool, SemanticVersion?, SemanticVersion?, SemanticVersion?, SemanticVersion?>(Commands.RunRootCommandHandler);
            return root;
        }

        private static class Commands
        {
            private static IAppTools GetSquirrelAppTools()
            {
                var m = new UpdateManager(updateSource: null, applicationIdOverride: null, localAppDataDirectoryOverride: null);
                m.Dispose();
                return m;
            }

            internal static int RunRootCommandHandler(bool squirrelFirstrun, SemanticVersion? squirrelInstall, SemanticVersion? squirrelUpdated, SemanticVersion? squirrelObsolete, SemanticVersion? squirrelUninstall)
            {
                var tools = GetSquirrelAppTools();
                if (squirrelInstall is not null)
                {
                    tools.CreateShortcutForThisExe();
                    Environment.Exit(0);
                    return 0;
                }
                else if (squirrelUpdated is not null)
                {
                    Environment.Exit(0);
                    return 0;
                }
                else if (squirrelObsolete is not null)
                {
                    Environment.Exit(0);
                    return 0;
                }
                else if (squirrelUninstall is not null)
                {
                    tools.RemoveShortcutForThisExe();
                    Environment.Exit(0);
                    return 0;
                }
                else
                {
                    tools.SetProcessAppUserModelId();
                    return RunWpfHandler(path: null, squirrelFirstrun: squirrelFirstrun, askPath: false, hide: false);
                }
            }

            internal static int RunWpfHandler(string? path, bool squirrelFirstrun, bool askPath, bool hide)
            {
                Pages.RootPage.CommandArgumentRecorderPath = path;
                Pages.RootPage.CommandArgumentFirstRun = squirrelFirstrun;
                Pages.RootPage.CommandArgumentAskPath = askPath;
                Pages.RootPage.CommandArgumentHide = hide;
                return CODE__WPF;
            }

            internal static int RunWpfReal()
            {
                var cancel = new CancellationTokenSource();
                var token = cancel.Token;
                try
                {
                    SleepBlocker.Start();

                    var app = new App();
                    app.InitializeComponent();
                    app.DispatcherUnhandledException += App_DispatcherUnhandledException;

                    updateTask = Task.Run(async () =>
                    {
                        while (!token.IsCancellationRequested)
                        {
                            await update.UpdateAsync().ConfigureAwait(false);
                            await Task.Delay(TimeSpan.FromDays(1), token).ConfigureAwait(false);
                        }
                    });

                    return app.Run();
                }
                finally
                {
                    cancel.Cancel();

                    StreamStartedNotification.Cleanup();

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                    update.WaitForUpdatesOnShutdownAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                }
            }
        }

        private static class SleepBlocker
        {
            internal static void Start()
            {
                var t = new Thread(EntryPoint)
                {
                    Name = "SystemSleepBlocker",
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal
                };
                t.Start();
            }

            [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

            [Flags]
            private enum EXECUTION_STATE : uint
            {
                ES_AWAYMODE_REQUIRED = 0x00000040,
                ES_CONTINUOUS = 0x80000000,
                ES_DISPLAY_REQUIRED = 0x00000002,
                ES_SYSTEM_REQUIRED = 0x00000001
            }

            private static void EntryPoint()
            {
                try
                {
                    while (true)
                    {
                        try
                        {
                            _ = SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);
                        }
                        catch (Exception) { }
                        Thread.Sleep(millisecondsTimeout: 30 * 1000);
                    }
                }
                catch (Exception) { }
            }
        }

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
                .WriteTo.Async(l => l.Sink<WpfLogEventSink>(Serilog.Events.LogEventLevel.Debug))
#else
                .WriteTo.Async(l => l.Sink<WpfLogEventSink>(Serilog.Events.LogEventLevel.Information))
#endif
                .WriteTo.Async(l => l.File(new CompactJsonFormatter(), logFilePath, shared: true, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true))
                .WriteTo.Sentry(o =>
                {
                    o.Dsn = "https://fed94ca0022e8697c171d0d35f15c257@o210546.ingest.sentry.io/5556540";
                    o.SendDefaultPii = true;
                    o.IsGlobalModeEnabled = true;
                    o.DisableAppDomainUnhandledExceptionCapture();
                    o.DisableUnobservedTaskExceptionCapture();
                    o.AddExceptionFilterForType<HttpRequestException>();
                    o.AddExceptionFilterForType<OutOfMemoryException>();
                    o.AddExceptionFilterForType<JintException>();
                    o.AddExceptionFilterForType<ParserException>();
                    o.AddEventProcessor(new SentryEventProcessor());

                    o.TextFormatter = new MessageTemplateTextFormatter("[{RoomId}] {Message}{NewLine}{Exception}{@ExceptionDetail:j}");

                    o.MinimumBreadcrumbLevel = Serilog.Events.LogEventLevel.Debug;
                    o.MinimumEventLevel = Serilog.Events.LogEventLevel.Error;

#if DEBUG
                    o.Environment = "debug-build";
#else
                    o.Environment = "release-build";
#endif
                })
                .CreateLogger();

        }

        [DllImport("kernel32")]
        private static extern bool AttachConsole(int pid);

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                logger.Fatal(ex, "Unhandled exception from AppDomain.UnhandledException");
        }

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e) =>
            logger.Error(e.Exception, "Unobserved exception from TaskScheduler.UnobservedTaskException");

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) =>
            logger.Fatal(e.Exception, "Unhandled exception from Application.DispatcherUnhandledException");

        private class SentryEventProcessor : ISentryEventProcessor
        {
            private const string JintConsole = "BililiveRecorder.Core.Scripting.Runtime.JintConsole";
            private static readonly string UserScriptRunner = typeof(Core.Scripting.UserScriptRunner).FullName;
            public SentryEvent? Process(SentryEvent e) => (e?.Logger == JintConsole || e?.Logger == UserScriptRunner) ? null : e;
        }
    }
}
