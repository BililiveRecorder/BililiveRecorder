using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.Pages
{
    public partial class ToolboxRemuxPage : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<ToolboxRemuxPage>();
        private string? selectedFilePath;

        public ToolboxRemuxPage()
        {
            this.InitializeComponent();
        }

        private async void SelectFile_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "选择FLV文件 Select FLV File",
                    AllowMultiple = false,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("FLV Files") { Patterns = new[] { "*.flv" } },
                        new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                    }
                });

                if (files.Count > 0)
                {
                    selectedFilePath = files[0].TryGetLocalPath();
                    SelectedFileText.Text = selectedFilePath ?? files[0].Name;
                    StartRemuxButton.IsEnabled = !string.IsNullOrEmpty(selectedFilePath);
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to select file");
                OutputText.Text = $"选择文件失败: {ex.Message}";
            }
        }

        private void StartRemux_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                OutputText.Text = "请先选择文件 Please select a file first";
                return;
            }

            // TODO: Implement remux functionality
            OutputText.Text = "Remux功能尚未实现 Remux feature not yet implemented\n";
            OutputText.Text += $"选择的文件: {selectedFilePath}";
        }
    }
}
