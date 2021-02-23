using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Sentry;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

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

        static Program()
        {
            levelSwitchGlobal = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Debug);
            levelSwitchConsole = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Error);
            logger = BuildLogger();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Log.Logger = logger;
            SentrySdk.ConfigureScope(s =>
            {
                try
                {
                    var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "..", "packages", ".betaId"));
                    if (!File.Exists(path)) return;
                    var content = File.ReadAllText(path);
                    if (Guid.TryParse(content, out var id)) s.User = new User { Id = id.ToString() };
                }
                catch (Exception) { }
            });
            update = new Update(logger);
        }

        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                logger.Debug("Starting, CurrentDirectory: {CurrentDirectory}, CommandLine: {CommandLine}", Environment.CurrentDirectory, Environment.CommandLine);
                var code = BuildCommand().Invoke(args);
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
                new Argument<string>("path","Work directory")

            };
            run.Handler = CommandHandler.Create((string path) => Commands.RunWpfHandler(path, false));

            var root = new RootCommand("")
            {
                run,
                new Option<bool>("--squirrel-firstrun")
                {
                    IsHidden = true
                }
            };
            root.Handler = CommandHandler.Create((bool squirrelFirstrun) => Commands.RunWpfHandler(null, squirrelFirstrun));
            return root;
        }

        private static class Commands
        {
            internal static int RunWpfHandler(string? path, bool squirrelFirstrun)
            {
                Pages.RootPage.CommandArgumentRecorderPath = path;
                Pages.RootPage.CommandArgumentFirstRun = squirrelFirstrun;
                return CODE__WPF;
            }

            internal static int RunWpfReal()
            {
                var cancel = new CancellationTokenSource();
                var token = cancel.Token;
                try
                {
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
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                    update.WaitForUpdatesOnShutdownAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                }
            }

            internal static int Tool_Fix(string path)
            {
                levelSwitchConsole.MinimumLevel = Serilog.Events.LogEventLevel.Information;
                // run code
                return 0;
            }

            internal static int Tool_Parse(string path)
            {
                levelSwitchConsole.MinimumLevel = Serilog.Events.LogEventLevel.Information;
                // run code
                return 0;
            }
        }

        private static Logger BuildLogger() => new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitchGlobal)
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.Debug()
            .WriteTo.Console(levelSwitch: levelSwitchConsole)
            .WriteTo.Sink<WpfLogEventSink>(Serilog.Events.LogEventLevel.Debug) // TODO level
            .WriteTo.File(new CompactJsonFormatter(), "./logs/bilirec.txt", shared: true, rollingInterval: RollingInterval.Day)
            .WriteTo.Sentry(o =>
            {
                o.Dsn = "https://efc16b0fd5604608b811c3b358e9d1f1@o210546.ingest.sentry.io/5556540";

                var v = typeof(Program).Assembly.GetName().Version;
                if (v.Major != 0)
                    o.Release = "BililiveRecorder@" + v.ToString();

                o.DisableAppDomainUnhandledExceptionCapture();
                o.AddExceptionFilterForType<System.Net.Http.HttpRequestException>();

                o.MinimumBreadcrumbLevel = Serilog.Events.LogEventLevel.Debug;
                o.MinimumEventLevel = Serilog.Events.LogEventLevel.Error;
                // TODO 测试调整 sentry
            })
            .CreateLogger();

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                logger.Fatal(ex, "Unhandled exception from Application.UnhandledException");
        }

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) =>
            logger.Fatal(e.Exception, "Unhandled exception from AppDomain.DispatcherUnhandledException");
    }
}
