using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BililiveRecorder.Desktop.Localization;
using BililiveRecorder.Desktop.Models;
using BililiveRecorder.Desktop.ViewModels;
using BililiveRecorder.ToolBox;
using BililiveRecorder.ToolBox.Tool.DanmakuMerger;
using BililiveRecorder.ToolBox.Tool.DanmakuStartTime;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.ViewModels
{
    /// <summary>弹幕合并页数据上下文。</summary>
    public class DanmakuMergerViewModel
    {
        public ILocalizationService Loc => LocalizationService.Instance;

        public ObservableCollection<DanmakuFileWithOffset> Files { get; } = new ObservableCollection<DanmakuFileWithOffset>();
    }
}

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    public partial class ToolboxDanmakuMergerView : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<ToolboxDanmakuMergerView>();
        private static readonly SemaphoreSlim singleOperation = new SemaphoreSlim(1, 1);

        public ToolboxDanmakuMergerView()
        {
            this.InitializeComponent();
            this.DataContext = new DanmakuMergerViewModel();
        }

        private ILocalizationService Loc => LocalizationService.Instance;

        private DanmakuMergerViewModel? Vm => this.DataContext as DanmakuMergerViewModel;

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Add_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null || this.Vm is null)
                    return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    AllowMultiple = true,
                    Title = this.Loc["Toolbox_Merge_OpenFileDialogTitle"],
                    FileTypeFilter = new[] { new FilePickerFileType("XML") { Patterns = new[] { "*.xml" } } },
                });

                await this.AddFilesAsync(files.Select(f => f.Path.LocalPath));
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error adding danmaku files");
            }
        }

        private async Task AddFilesAsync(IEnumerable<string> paths)
        {
            var vm = this.Vm;
            if (vm is null)
                return;

            var newPaths = paths
                .Where(p => vm.Files.All(x => x.Path != p))
                .ToArray();
            if (newPaths.Length == 0)
                return;

            var resp = await new DanmakuStartTimeHandler().Handle(
                new DanmakuStartTimeRequest { Inputs = newPaths },
                default,
                null);

            if (resp.Status != ResponseStatus.OK || resp.Data is null)
            {
                this.ShowStatus(this.LocalizeStatus(resp.Status) + "\n" + resp.ErrorMessage);
                return;
            }

            var offsetBase = vm.Files.Count > 0
                ? vm.Files.Min(x => x.StartTime)
                : resp.Data.StartTimes.Min(x => x.StartTime);

            foreach (var item in resp.Data.StartTimes)
            {
                vm.Files.Add(new DanmakuFileWithOffset
                {
                    Path = item.Path,
                    StartTime = item.StartTime,
                    Offset = (int)(item.StartTime - offsetBase).TotalSeconds,
                });
            }
        }

        private void Remove_Click(object? sender, RoutedEventArgs e)
        {
            if (this.Vm is not { } vm)
                return;

            var selected = this.FilesList.SelectedItems?.Cast<DanmakuFileWithOffset>().ToArray();
            if (selected is null)
                return;

            foreach (var item in selected)
                vm.Files.Remove(item);
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Merge_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                var vm = this.Vm;
                if (vm is null)
                    return;

                if (vm.Files.Count < 2)
                {
                    this.ShowStatus(this.Loc["Toolbox_Merge_Error_AtLeastTwo"]);
                    return;
                }

                if (!await singleOperation.WaitAsync(0))
                {
                    this.ShowStatus("已有任务在运行 Operation already running");
                    return;
                }

                try
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel is null)
                        return;

                    var output = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        SuggestedFileName = "merged.xml",
                        FileTypeChoices = new[] { new FilePickerFileType("XML") { Patterns = new[] { "*.xml" } } },
                    });
                    var outputPath = output?.Path.LocalPath;
                    if (string.IsNullOrEmpty(outputPath))
                        return;

                    var progressWindow = new ToolProgressWindow();
                    var mergerOwner = App.MainWindowOrNull;
                    if (mergerOwner is not null)
                        progressWindow.Show(mergerOwner);
                    else
                        progressWindow.Show();
                    try
                    {
                        var files = vm.Files.ToArray();
                        var resp = await new DanmakuMergerHandler().Handle(
                            new DanmakuMergerRequest
                            {
                                Inputs = files.Select(x => x.Path).ToArray(),
                                Offsets = files.Select(x => x.Offset).ToArray(),
                                Output = outputPath,
                            },
                            progressWindow.Token,
                            p => { progressWindow.Report(p * 98); return Task.CompletedTask; });

                        this.ShowStatus(resp.Status == ResponseStatus.OK
                            ? "✓ " + outputPath
                            : this.LocalizeStatus(resp.Status) + "\n" + resp.ErrorMessage);

                        if (resp.Status == ResponseStatus.OK)
                            vm.Files.Clear();
                    }
                    catch (OperationCanceledException)
                    {
                        this.ShowStatus("已取消 Cancelled");
                    }
                    finally
                    {
                        await Dispatcher.UIThread.InvokeAsync(progressWindow.Close);
                    }
                }
                finally
                {
                    singleOperation.Release();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error merging danmaku files");
                this.ShowStatus("Error: " + ex.Message);
            }
        }

        private string LocalizeStatus(ResponseStatus status) => status switch
        {
            ResponseStatus.OK => "✓ " + this.Loc["Toolbox_AutoFix_Error_Type_OK"],
            ResponseStatus.Cancelled => "已取消 Cancelled",
            _ => this.Loc["Toolbox_AutoFix_Error_Type_" + status],
        };

        private void ShowStatus(string text)
        {
            Dispatcher.UIThread.Post(() => this.StatusText.Text = text);
        }
    }
}
