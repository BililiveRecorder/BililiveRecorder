using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Templating;
using Newtonsoft.Json.Linq;

#nullable enable
namespace BililiveRecorder.Desktop.Pages
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private void TestFileNameTemplate_Button_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var template = FileNameTemplateTextBox.Text ?? "";
                var context = new FileNameTemplateContext
                {
                    RoomId = 12345,
                    ShortId = 123,
                    Name = "TestUser",
                    Title = "Test Stream Title",
                    AreaParent = "Games",
                    AreaChild = "Minecraft",
                    Qn = 10000,
                    Json = new JObject()
                };

                // Create a simple config to test the template
                var config = new TestFileNameConfig { FileNameRecordTemplate = template };
                var generator = new FileNameGenerator(config, null);
                var result = generator.CreateFilePath(context);

                FileNameTestResultArea.IsVisible = true;
                FileNameTestErrorText.Text = result.ErrorMessage ?? "";
                FileNameTestResultText.Text = result.RelativePath ?? "";
            }
            catch (Exception ex)
            {
                FileNameTestResultArea.IsVisible = true;
                FileNameTestErrorText.Text = ex.Message;
                FileNameTestResultText.Text = "";
            }
        }

        private class TestFileNameConfig : IFileNameConfig
        {
            public string? WorkDirectory => null;
            public string FileNameRecordTemplate { get; set; } = "";
        }

        private void OpenFileNameHelp_Click(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://rec.danmuji.org/docs/basic/settings/#%E5%BD%95%E5%88%B6%E6%96%87%E4%BB%B6%E5%90%8D%E6%A0%BC%E5%BC%8F");
        }

        private void OpenQualityHelp_Click(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://rec.danmuji.org/docs/basic/settings/#%E7%9B%B4%E6%92%AD%E7%94%BB%E8%B4%A8");
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
            catch (Exception) { }
        }
    }
}
