using System;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Scripting;
using Newtonsoft.Json.Linq;
using Serilog;

#nullable enable
namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for AdvancedSettingsPage.xaml
    /// </summary>
    public partial class AdvancedSettingsPage
    {
        private static readonly ILogger logger = Log.ForContext<AdvancedSettingsPage>();
        private readonly IHttpClientAccessor? httpApiClient;
        private readonly UserScriptRunner? userScriptRunner;

        public AdvancedSettingsPage(IHttpClientAccessor? httpApiClient, UserScriptRunner? userScriptRunner)
        {
            this.InitializeComponent();
            this.httpApiClient = httpApiClient;
            this.userScriptRunner = userScriptRunner;
        }

        public AdvancedSettingsPage()
            : this(
                  (IHttpClientAccessor?)(RootPage.ServiceProvider?.GetService(typeof(IHttpClientAccessor))),
                  (UserScriptRunner?)(RootPage.ServiceProvider?.GetService(typeof(UserScriptRunner)))
                  )
        { }

        private void Crash_Click(object sender, RoutedEventArgs e) => throw new TestException("test crash triggered");

        public class TestException : Exception
        {
            public TestException() { }
            public TestException(string message) : base(message) { }
            public TestException(string message, Exception innerException) : base(message, innerException) { }
            protected TestException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        private void Throw_In_Task_Click(object sender, RoutedEventArgs e) => _ = Task.Run(() =>
        {
            throw new TestException("test task exception triggered");
        });

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void TestCookie_Click(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                await this.TestCookieAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Exception in TestCookie");
                MessageBox.Show(ex.ToString(), "Cookie Test - Error", MessageBoxButton.OK);
            }
        }

        private async Task TestCookieAsync()
        {
            bool succeed;
            string message;

            if (this.httpApiClient is null)
                (succeed, message) = (false, "No Http Client Available");
            else
                (succeed, message) = await this.httpApiClient.TestCookieAsync().ConfigureAwait(false);

            if (succeed)
                MessageBox.Show(message, "Cookie Test - Succeed", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show(message, "Cookie Test - Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void TestScript_Click(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(() => this.userScriptRunner?.CallOnTest(Log.Logger, str => MessageBox.Show(str)));
        }
    }
}
