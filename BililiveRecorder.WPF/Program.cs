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
                    o.Dsn = new Dsn("https://55afa848ac49493a80cc4366b34e9552@o210546.ingest.sentry.io/5556540");
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
