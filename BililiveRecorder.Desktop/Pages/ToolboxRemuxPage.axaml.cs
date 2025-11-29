using System;
using System.Collections.Generic;
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

        public ToolboxRemuxPage()
        {
            this.InitializeComponent();
        }

        private async void SelectFile_Button_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "选择 FLV 文件",
                        AllowMultiple = false,
                        FileTypeFilter = new List<FilePickerFileType>
                        {
                            new FilePickerFileType("FLV 文件") { Patterns = new[] { "*.flv" } }
                        }
                    });

                    if (files.Count > 0)
                    {
                        this.FileNameTextBox.Text = files[0].Path.LocalPath;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to select file");
            }
        }

        private void Remux_Button_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Implement remux
        }
    }
}
