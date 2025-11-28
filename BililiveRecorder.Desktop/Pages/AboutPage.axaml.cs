using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

#nullable enable
namespace BililiveRecorder.Desktop.Pages
{
    public partial class AboutPage : UserControl
    {
        public string VersionText => $"Version: {GitVersionInformation.InformationalVersion}";

        public AboutPage()
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.CopyrightTextBlock.Text = $"Copyright © {DateTime.Now.Year} Genteure";
        }

        private void OpenEmail_Click(object? sender, RoutedEventArgs e)
        {
            OpenUrl("mailto:rec@danmuji.org");
        }

        private void OpenWebsite_Click(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://rec.danmuji.org");
        }

        private void OpenGitHub_Click(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/BililiveRecorder/BililiveRecorder");
        }

        private static void OpenUrl(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            }
            catch (Exception)
            {
                // Failed to open URL
            }
        }
    }
}
