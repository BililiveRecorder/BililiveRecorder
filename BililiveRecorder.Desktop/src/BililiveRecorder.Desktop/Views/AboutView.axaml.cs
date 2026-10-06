using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BililiveRecorder.Desktop.Localization;

#nullable enable
namespace BililiveRecorder.Desktop.ViewModels
{
    public class AboutViewModel
    {
        public ILocalizationService Loc => LocalizationService.Instance;

        public string VersionText => string.Format(LocalizationService.Instance["About_Version"], GitVersionInformation.InformationalVersion);
    }
}

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    public partial class AboutView : UserControl
    {
        public AboutView()
        {
            this.InitializeComponent();
            this.DataContext = new ViewModels.AboutViewModel();
        }

        private void OpenLink_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as Control)?.Tag is string url)
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception)
            {
            }
        }
    }
}
