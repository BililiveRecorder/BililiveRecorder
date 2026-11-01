using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Scripting;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    public partial class AdvancedSettingsView : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<AdvancedSettingsView>();

        public AdvancedSettingsView()
        {
            this.InitializeComponent();
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void TestScriptButton_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                if (this.DataContext is not ViewModels.SettingsViewModel vm)
                    return;

                var runner = App.ServicesOrNull?.GetService(typeof(UserScriptRunner)) as UserScriptRunner;
                if (runner is null)
                    return;

                string? result = null;
                runner.CallOnTest(Log.Logger, s => result = s);

                this.ShowResult(result ?? "(无输出 no output)");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error testing user script");
                this.ShowResult("Error: " + ex.Message);
            }
            await Task.CompletedTask;
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void TestCookieButton_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                var tester = App.ServicesOrNull?.GetService(typeof(ICookieTester)) as ICookieTester;
                if (tester is null)
                    return;

                var (ok, message) = await tester.TestCookieAsync();
                this.ShowResult((ok ? "✓ " : "✗ ") + message);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error testing cookie");
                this.ShowResult("Error: " + ex.Message);
            }
        }

        private void ShowResult(string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                this.CookieTestResult.Text = message;
                this.CookieTestResult.IsVisible = true;
            });
        }
    }
}
