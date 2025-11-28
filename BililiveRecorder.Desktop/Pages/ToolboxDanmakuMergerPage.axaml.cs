using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.Pages
{
    public partial class ToolboxDanmakuMergerPage : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<ToolboxDanmakuMergerPage>();
        private List<string> selectedFilePaths = new List<string>();

        public ToolboxDanmakuMergerPage()
        {
            this.InitializeComponent();
        }

        private async void SelectFiles_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "选择XML文件 Select XML Files",
                    AllowMultiple = true,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("XML Files") { Patterns = new[] { "*.xml" } },
                        new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                    }
                });

                if (files.Count > 0)
                {
                    selectedFilePaths = files.Select(f => f.TryGetLocalPath() ?? f.Name).ToList();
                    SelectedFilesText.Text = $"已选择 {selectedFilePaths.Count} 个文件 / {selectedFilePaths.Count} files selected";
                    StartMergeButton.IsEnabled = selectedFilePaths.Count > 0;
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to select files");
                OutputText.Text = $"选择文件失败: {ex.Message}";
            }
        }

        private void StartMerge_Click(object? sender, RoutedEventArgs e)
        {
            if (selectedFilePaths.Count == 0)
            {
                OutputText.Text = "请先选择文件 Please select files first";
                return;
            }

            // TODO: Implement merge functionality
            OutputText.Text = "弹幕合并功能尚未实现 Danmaku merge feature not yet implemented\n";
            OutputText.Text += $"选择的文件数: {selectedFilePaths.Count}\n";
            foreach (var path in selectedFilePaths)
            {
                OutputText.Text += $"  - {path}\n";
            }
        }
    }
}
