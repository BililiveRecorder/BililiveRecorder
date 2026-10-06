using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BililiveRecorder.Desktop.Localization;
using BililiveRecorder.Desktop.ViewModels;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.ToolBox;
using BililiveRecorder.ToolBox.Tool.Analyze;
using BililiveRecorder.ToolBox.Tool.Export;
using BililiveRecorder.ToolBox.Tool.Fix;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.ViewModels
{
    /// <summary>工具箱页面的基础数据上下文（本地化）。</summary>
    public class ToolboxViewModel
    {
        public ILocalizationService Loc => LocalizationService.Instance;
    }
}

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    public partial class ToolboxAutoFixView : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<ToolboxAutoFixView>();
        private static readonly SemaphoreSlim singleOperation = new SemaphoreSlim(1, 1);

        private bool running;

        public ToolboxAutoFixView()
        {
            this.InitializeComponent();
            this.DataContext = new ToolboxViewModel();
        }

        private ILocalizationService Loc => LocalizationService.Instance;

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void BrowseInput_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                var files = await this.PickFileAsync("FLV / XML / GZ / ZIP", new[] { "*.flv", "*.xml", "*.gz", "*.zip" });
                if (files is { Count: > 0 })
                    this.InputPathBox.Text = files[0];
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error picking input file");
            }
        }

        private async Task<string?> PickOutputAsync(string defaultName, string pattern, string description)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
                return null;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = defaultName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(description) { Patterns = new[] { pattern } },
                },
            });
            return file?.Path.LocalPath;
        }

        private async Task<System.Collections.Generic.List<string>?> PickFileAsync(string description, string[] patterns)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is null)
                return null;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = this.Loc["Toolbox_AutoFix_SelectInputDialog_Title"],
                FileTypeFilter = new[] { new FilePickerFileType(description) { Patterns = patterns } },
            });

            var result = new System.Collections.Generic.List<string>();
            foreach (var f in files)
                result.Add(f.Path.LocalPath);
            return result;
        }

        private ProcessingPipelineSettings ReadPipelineSettings() => new ProcessingPipelineSettings
        {
            SplitOnScriptTag = this.SplitOnScriptTagBox.IsChecked == true,
            DisableSplitOnH264AnnexB = this.DisableSplitOnH264AnnexBBox.IsChecked == true,
        };

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Analyze_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            var input = this.InputPathBox.Text?.Trim();
            if (string.IsNullOrEmpty(input) || !await this.TryBeginOperationAsync())
                return;

            var settings = this.ReadPipelineSettings();
            await this.RunOperationWithProgressAsync(async (token, progress) =>
            {
                var resp = await new AnalyzeHandler().Handle(
                    new AnalyzeRequest { Input = input, PipelineSettings = settings },
                    token,
                    p => { progress.Report(p * 98); return Task.CompletedTask; });

                if (resp.Status == ResponseStatus.OK && resp.Data is not null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => this.ShowAnalyzeResult(resp.Data));
                }
                else
                {
                    this.ShowStatus(this.LocalizeStatus(resp));
                }
            });
        }

        private void ShowAnalyzeResult(AnalyzeResponse data)
        {
            var lines = new System.Collections.Generic.List<string>
            {
                $"{this.Loc["Toolbox_AutoFix_AnalyzeResult_FixNeeded"]}: {(data.NeedFix ? "✓" : "✗")}",
                $"{this.Loc["Toolbox_AutoFix_AnalyzeResult_ContainsUnrepairable"]}: {(data.Unrepairable ? "✓" : "✗")}",
                $"{this.Loc["Toolbox_AutoFix_AnalyzeResult_OutputFileCount"]}: {data.OutputFileCount}",
                $"{this.Loc["Toolbox_AutoFix_AnalyzeResult_IssueTypeTimestampOffset"]}: {data.IssueTypeTimestampOffset}",
                $"{this.Loc["Toolbox_AutoFix_AnalyzeResult_IssueTypeTimestampJump"]}: {data.IssueTypeTimestampJump}",
                $"{this.Loc["Toolbox_AutoFix_AnalyzeResult_IssueTypeDecodingHeader"]}: {data.IssueTypeDecodingHeader}",
                $"{this.Loc["Toolbox_AutoFix_AnalyzeResult_IssueTypeRepeatingData"]}: {data.IssueTypeRepeatingData}",
                $"{this.Loc["Toolbox_AutoFix_AnalyzeResult_IssueTypeOther"]}: {data.IssueTypeOther}",
                $"{this.Loc["Toolbox_AutoFix_AnalyzeResult_IssueTypeUnrepairable"]}: {data.IssueTypeUnrepairable}",
            };

            if (data.VideoStats is { } video)
                lines.Add($"Video: {video.FrameCount} frames, {video.FramePerSecond:F2} fps");
            if (data.AudioStats is { } audio)
                lines.Add($"Audio: {audio.FrameCount} frames, {audio.FramePerSecond:F2} fps");

            this.ResultText.Text = string.Join("\n", lines);
            this.ResultText.IsVisible = true;
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Fix_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            var input = this.InputPathBox.Text?.Trim();
            if (string.IsNullOrEmpty(input))
                return;

            try
            {
                var output = await this.PickOutputAsync(
                    Path.GetFileNameWithoutExtension(input) + "_fixed",
                    "*.flv",
                    "FLV");
                if (string.IsNullOrEmpty(output))
                    return;

                if (!await this.TryBeginOperationAsync())
                    return;

                var settings = this.ReadPipelineSettings();
                await this.ExecuteOperationAsync(async (token, progress) =>
                {
                    var resp = await new FixHandler().Handle(
                        new FixRequest { Input = input, OutputBase = output, PipelineSettings = settings },
                        token,
                        p => { progress.Report(p * 98); return Task.CompletedTask; });

                    this.ShowStatus(resp.Status == ResponseStatus.OK
                        ? "✓ " + this.Loc["Toolbox_AutoFix_Error_Type_OK"]
                        : this.LocalizeStatus(resp));
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fixing file");
                this.running = false;
                this.SetButtons(true);
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Export_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            var input = this.InputPathBox.Text?.Trim();
            if (string.IsNullOrEmpty(input))
                return;

            try
            {
                var output = await this.PickOutputAsync(
                    Path.GetFileNameWithoutExtension(input) + ".brec.xml",
                    "*.xml.zip",
                    "XML ZIP");
                if (string.IsNullOrEmpty(output))
                    return;

                if (!await this.TryBeginOperationAsync())
                    return;

                await this.ExecuteOperationAsync(async (token, progress) =>
                {
                    var resp = await new ExportHandler().Handle(
                        new ExportRequest { Input = input, Output = output },
                        token,
                        p => { progress.Report(p * 95); return Task.CompletedTask; });

                    this.ShowStatus(resp.Status == ResponseStatus.OK
                        ? "✓ " + this.Loc["Toolbox_AutoFix_Error_Type_OK"]
                        : this.LocalizeStatus(resp));
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error exporting file data");
                this.running = false;
                this.SetButtons(true);
            }
        }

        private async Task<bool> TryBeginOperationAsync()
        {
            if (this.running || !await singleOperation.WaitAsync(0))
            {
                this.ShowStatus("已有任务在运行 Operation already running");
                return false;
            }

            this.running = true;
            this.SetButtons(false);
            this.ResultText.IsVisible = false;
            this.ShowStatus(string.Empty);
            return true;
        }

        private async Task RunOperationWithProgressAsync(Func<CancellationToken, ToolProgressWindow, Task> action)
        {
            try
            {
                await this.ExecuteOperationAsync(action);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error running tool operation");
                this.ShowStatus("Error: " + ex.Message);
                this.running = false;
                this.SetButtons(true);
            }
        }

        private async Task ExecuteOperationAsync(Func<CancellationToken, ToolProgressWindow, Task> action)
        {
            var progressWindow = new ToolProgressWindow();
            var owner = App.MainWindowOrNull;
            if (owner is not null)
                progressWindow.Show(owner);
            else
                progressWindow.Show();
            try
            {
                await action(progressWindow.Token, progressWindow);
            }
            catch (OperationCanceledException)
            {
                this.ShowStatus("已取消 Cancelled");
            }
            finally
            {
                await Dispatcher.UIThread.InvokeAsync(progressWindow.Close);
                singleOperation.Release();
                this.running = false;
                this.SetButtons(true);
            }
        }

        private string LocalizeStatus<T>(CommandResponse<T> resp) where T : class, BililiveRecorder.ToolBox.IResponseData
        {
            var message = resp.Status switch
            {
                ResponseStatus.Cancelled => "已取消 Cancelled",
                _ => this.Loc["Toolbox_AutoFix_Error_Type_" + resp.Status],
            };
            if (!string.IsNullOrEmpty(resp.ErrorMessage))
                message += "\n" + resp.ErrorMessage;
            return message;
        }

        private void ShowStatus(string text)
        {
            Dispatcher.UIThread.Post(() =>
            {
                this.StatusText.Text = text;
            });
        }

        private void SetButtons(bool enabled)
        {
            Dispatcher.UIThread.Post(() =>
            {
                this.AnalyzeButton.IsEnabled = enabled;
                this.FixButton.IsEnabled = enabled;
                this.ExportButton.IsEnabled = enabled;
            });
        }
    }
}
