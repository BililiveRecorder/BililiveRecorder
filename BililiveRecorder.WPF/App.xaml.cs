using System;
using System.Windows;
using Microsoft.Win32;
using Serilog;

#nullable enable
namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly ILogger loggerSystemEvents = Log.ForContext<SystemEvents>();

        private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            if (e != null)
                e.Cancel = true;
            (Current.MainWindow as NewMainWindow)?.CloseWithoutConfirmAction();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                SystemEvents.TimeChanged += this.SystemEvents_TimeChanged;
                SystemEvents.SessionEnding += this.SystemEvents_SessionEnding;
                SystemEvents.PowerModeChanged += this.SystemEvents_PowerModeChanged;
            }
            catch (Exception ex)
            {
                this.loggerSystemEvents.Error(ex, "Error attaching event");
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                SystemEvents.TimeChanged -= this.SystemEvents_TimeChanged;
                SystemEvents.SessionEnding -= this.SystemEvents_SessionEnding;
                SystemEvents.PowerModeChanged -= this.SystemEvents_PowerModeChanged;
            }
            catch (Exception ex)
            {
                this.loggerSystemEvents.Error(ex, "Error detaching event");
            }
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e) => this.loggerSystemEvents.Debug("SessionEnding {Reason}", e.Reason);

        private void SystemEvents_TimeChanged(object sender, System.EventArgs e) => this.loggerSystemEvents.Debug("TimeChanged");

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e) => this.loggerSystemEvents.Debug("PowerModeChanged {Mode}", e.Mode);
    }
}
