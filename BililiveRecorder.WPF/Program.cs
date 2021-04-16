using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Windows.Threading;
using NLog;
using Sentry;

namespace BililiveRecorder.WPF
{
    public static class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [STAThread]
        public static int Main()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            using (SentrySdk.Init(o =>
            {
                if (!File.Exists("BILILIVE_RECORDER_DISABLE_SENTRY")
                && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BILILIVE_RECORDER_DISABLE_SENTRY")))
                {
                    o.Dsn = new Dsn("https://74f6e28f6a8848dabbb96c5b5c51c4c2@o210546.ingest.sentry.io/5556540");
                }

                var v = typeof(Program).Assembly.GetName().Version;
                if (v.Major != 0)
                {
                    o.Release = "BililiveRecorder@" + v.ToString();
                }
                o.DisableAppDomainUnhandledExceptionCapture();
                o.AddExceptionFilterForType<System.Net.Http.HttpRequestException>();
            }))
            {
                SentrySdk.ConfigureScope(s =>
                {
                    try
                    {
                        var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "..", "packages", ".betaId"));
                        if (!File.Exists(path)) return;
                        var content = File.ReadAllText(path);
                        if (Guid.TryParse(content, out var id))
                            s.User = new Sentry.Protocol.User { Id = id.ToString() };
                    }
                    catch (Exception) { }
                });

                var app = new App();
                app.InitializeComponent();
                app.DispatcherUnhandledException += App_DispatcherUnhandledException;
                return app.Run();
            }
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            LogManager.Flush();
            LogManager.Shutdown();
        }

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                logger.Fatal(ex, "Unhandled exception from Application.UnhandledException");
                LogManager.Flush();
            }
        }

        [HandleProcessCorruptedStateExceptions, SecurityCritical]
        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            logger.Fatal(e.Exception, "Unhandled exception from AppDomain.DispatcherUnhandledException");
            LogManager.Flush();
        }
    }
}
