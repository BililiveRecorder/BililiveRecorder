using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop
{
    public partial class App : Application
    {
        private readonly ILogger loggerSystemEvents = Log.ForContext("SourceContext", "SystemEvents");

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                desktop.ShutdownRequested += Desktop_ShutdownRequested;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Desktop_ShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            // Handle shutdown
            loggerSystemEvents.Debug("Application shutdown requested");
        }
    }
}