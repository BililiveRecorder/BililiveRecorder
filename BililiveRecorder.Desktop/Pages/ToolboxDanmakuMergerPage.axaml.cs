using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly ObservableCollection<string> files = new ObservableCollection<string>();

        public ToolboxDanmakuMergerPage()
        {
            this.InitializeComponent();
            this.FileListBox.ItemsSource = this.files;
        }

        private async void AddFile_Button_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    var selectedFiles = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "选择弹幕文件",
                        AllowMultiple = true,
                        FileTypeFilter = new List<FilePickerFileType>
                        {
                            new FilePickerFileType("XML 文件") { Patterns = new[] { "*.xml" } }
                        }
                    });

                    foreach (var file in selectedFiles)
                    {
                        this.files.Add(file.Path.LocalPath);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to select files");
            }
        }

        private void ClearList_Button_Click(object? sender, RoutedEventArgs e)
        {
            this.files.Clear();
        }

        private void Merge_Button_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Implement merge
        }
    }
}
