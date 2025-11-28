using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BililiveRecorder.ToolBox;
using BililiveRecorder.ToolBox.Tool.Fix;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.Pages
{
    public partial class ToolboxAutoFixPage : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<ToolboxAutoFixPage>();
        private string? selectedFilePath;

        public ToolboxAutoFixPage()
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
                    StartFixButton.IsEnabled = !string.IsNullOrEmpty(selectedFilePath);
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to select file");
                OutputText.Text = $"选择文件失败: {ex.Message}";
            }
        }

        private async void StartFix_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                OutputText.Text = "请先选择文件 Please select a file first";
                return;
            }

            try
            {
                StartFixButton.IsEnabled = false;
                SelectFileButton.IsEnabled = false;
                OutputText.Text = "正在修复... Fixing...\n";

                var outputPath = Path.ChangeExtension(selectedFilePath, ".fixed.flv");
                
                var request = new FixRequest
                {
                    Input = selectedFilePath,
                    OutputBase = Path.GetDirectoryName(outputPath) ?? ".",
                };

                var handler = new FixHandler();
                var response = await Task.Run(async () =>
                {
                    return await handler.Handle(request, default, async p =>
                    {
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            OutputText.Text += $"进度: {p * 100:F1}%\n";
                        });
                    });
                });

                if (response.Status == ResponseStatus.OK)
                {
                    OutputText.Text += $"\n修复完成 Fix complete!\n";
                    OutputText.Text += $"输出文件: {response.Data?.OutputFileCount ?? 0} 个文件\n";
                }
                else
                {
                    OutputText.Text += $"\n修复失败: {response.Status}\n";
                    if (!string.IsNullOrEmpty(response.ErrorMessage))
                    {
                        OutputText.Text += $"错误: {response.ErrorMessage}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to fix file");
                OutputText.Text += $"\n修复失败: {ex.Message}\n";
            }
            finally
            {
                StartFixButton.IsEnabled = true;
                SelectFileButton.IsEnabled = true;
            }
        }
    }
}
