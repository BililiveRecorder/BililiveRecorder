using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.Pages
{
    public partial class AnnouncementPage : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<AnnouncementPage>();
        public static CultureInfo CultureInfo { get; set; } = CultureInfo.CurrentUICulture;

        public AnnouncementPage()
        {
            this.InitializeComponent();
            this.Loaded += AnnouncementPage_Loaded;
        }

        private async void AnnouncementPage_Loaded(object? sender, RoutedEventArgs e)
        {
            await LoadAnnouncementAsync();
        }

        private async void Button_Click(object? sender, RoutedEventArgs e)
        {
            await LoadAnnouncementAsync();
        }

        private async Task LoadAnnouncementAsync()
        {
            this.Loading.IsVisible = true;
            this.Error.IsVisible = false;
            this.Container.IsVisible = false;

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.GetStringAsync("https://rec.danmuji.org/announcement/desktop.txt");

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.ContentTextBlock.Text = response;
                    this.Container.IsVisible = true;
                    this.Loading.IsVisible = false;
                });
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to load announcement");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.Error.IsVisible = true;
                    this.Loading.IsVisible = false;
                });
            }
        }
    }
}
