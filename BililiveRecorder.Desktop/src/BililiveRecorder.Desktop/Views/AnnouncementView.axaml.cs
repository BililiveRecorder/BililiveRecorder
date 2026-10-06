using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BililiveRecorder.Desktop.Localization;
using BililiveRecorder.Desktop.ViewModels;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.ViewModels
{
    public class AnnouncementViewModel
    {
        public ILocalizationService Loc => LocalizationService.Instance;
    }
}

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    /// <summary>
    /// 公告页。WPF 版通过远程下载并渲染 WPF XAML，Avalonia 无法渲染 XAML，
    /// 因此抓取同一地址后提取其中的文本内容显示。
    /// </summary>
    public partial class AnnouncementView : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<AnnouncementView>();
        private static readonly HttpClient client = new HttpClient();
        private static readonly Regex textElementRegex = new Regex(@"<t[^>]*?Text=""([^""]*)""", RegexOptions.Compiled);

        private static string? cachedAnnouncement;

        public AnnouncementView()
        {
            this.InitializeComponent();
            this.DataContext = new AnnouncementViewModel();
            _ = this.LoadAnnouncementAsync(ignoreCache: false);
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Refresh_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            await this.LoadAnnouncementAsync(ignoreCache: true);
        }

        private async Task LoadAnnouncementAsync(bool ignoreCache)
        {
            this.ErrorText.IsVisible = false;
            this.AnnouncementText.Text = string.Empty;

            try
            {
                if (!ignoreCache && cachedAnnouncement is not null)
                {
                    this.AnnouncementText.Text = cachedAnnouncement;
                    return;
                }

                var culture = LocalizationService.Instance.CurrentCulture.Name;
                var uri = $"https://rec.danmuji.org/wpf/announcement.xml?c={culture}";
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                request.Headers.UserAgent.ParseAdd($"BililiveRecorder/{GitVersionInformation.FullSemVer}");

                using var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                // 从 WPF XAML 中提取 Text="..." 文本内容
                var matches = textElementRegex.Matches(content);
                var text = string.Join("\n", matches.Select(m => Unescape(m.Groups[1].Value)));

                cachedAnnouncement = string.IsNullOrWhiteSpace(text) ? null : text;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.AnnouncementText.Text = cachedAnnouncement ?? string.Empty;
                    this.ErrorText.IsVisible = cachedAnnouncement is null;
                });
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to load announcement");
                await Dispatcher.UIThread.InvokeAsync(() => this.ErrorText.IsVisible = true);
            }
        }

        private static string Unescape(string s) => s
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&#x0a;", "\n");
    }
}
