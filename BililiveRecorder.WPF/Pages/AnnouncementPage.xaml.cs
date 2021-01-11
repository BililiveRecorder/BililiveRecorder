using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xaml;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for AnnouncementPage.xaml
    /// </summary>
    public partial class AnnouncementPage
    {
        private static readonly HttpClient client;

        private static MemoryStream AnnouncementCache = null;
        private static DateTimeOffset AnnouncementCacheTime = DateTimeOffset.MinValue;
        internal static CultureInfo CultureInfo = CultureInfo.CurrentUICulture;

        static AnnouncementPage()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", $"BililiveRecorder/{typeof(AnnouncementPage).Assembly.GetName().Version}-{BuildInfo.HeadShaShort}");
        }

        public AnnouncementPage()
        {
            this.InitializeComponent();
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(async () => await this.LoadAnnouncementAsync(ignore_cache: false, show_error: false)));
        }

        private async void Button_Click(object sender, RoutedEventArgs e) => await this.LoadAnnouncementAsync(ignore_cache: true, show_error: Keyboard.Modifiers.HasFlag(ModifierKeys.Control));

        private async Task LoadAnnouncementAsync(bool ignore_cache, bool show_error)
        {
            MemoryStream data;
            bool success;

            this.Container.Child = null;
            this.Error.Visibility = Visibility.Collapsed;
            this.Loading.Visibility = Visibility.Visible;

            if (AnnouncementCache is not null && !ignore_cache)
            {
                data = AnnouncementCache;
                success = true;
            }
            else
            {
                try
                {
#if DEBUG
                    var uri = $"http://rec.127-0-0-1.nip.io/wpf/announcement.php?c={CultureInfo.Name}";
#else
                    var uri = $"https://rec.danmuji.org/wpf/announcement.xml?c={CultureInfo.Name}";
#endif
                    var resp = await client.GetAsync(uri);
                    var stream = await resp.EnsureSuccessStatusCode().Content.ReadAsStreamAsync();
                    var mstream = new MemoryStream();
                    await stream.CopyToAsync(mstream);
                    AnnouncementCacheTime = DateTimeOffset.Now;
                    data = mstream;
                    success = true;
                }
                catch (Exception ex)
                {
                    data = null;
                    success = false;
                    if (show_error) MessageBox.Show(ex.ToString(), "Loading Error");
                }
            }

            if (success)
            {
                try
                {
                    using var stream = new MemoryStream();
                    data.Position = 0;
                    await data.CopyToAsync(stream);
                    stream.Position = 0;
                    using var reader = new XamlXmlReader(stream, System.Windows.Markup.XamlReader.GetWpfSchemaContext());
                    var obj = System.Windows.Markup.XamlReader.Load(reader);
                    if (obj is UIElement elem)
                        this.Container.Child = elem;
                }
                catch (Exception ex)
                {
                    data = null;
                    success = false;
                    if (show_error) MessageBox.Show(ex.ToString(), "Loading Error");
                }
            }

            this.Loading.Visibility = Visibility.Collapsed;
            if (success)
            {
                this.RefreshButton.ToolTip = "Load Time: " + AnnouncementCacheTime.ToString("F");
                AnnouncementCache = data;
            }
            else
            {
                this.RefreshButton.ToolTip = null;
                this.Error.Visibility = Visibility.Visible;
            }
        }

        private async void Button_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) return;

            var fileDialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = false,
                Multiselect = false,
                Title = "Load local file",
                AddToMostRecentlyUsedList = false,
                EnsurePathExists = true,
                NavigateToShortcut = true,
            };
            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    var ms = new MemoryStream();
                    using (var fs = File.OpenRead(fileDialog.FileName))
                    {
                        await fs.CopyToAsync(ms);
                    }
                    AnnouncementCache = ms;
                    AnnouncementCacheTime = DateTimeOffset.Now;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Loading Error");
                }
                await this.LoadAnnouncementAsync(ignore_cache: false, show_error: true);
            }
        }
    }
}
