using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BililiveRecorder.ToolBox;
using BililiveRecorder.ToolBox.Tool.DanmakuMerger;
using BililiveRecorder.WPF.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using WPFLocalizeExtension.Extensions;

#nullable enable
namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for ToolboxDanmakuMergerPage.xaml
    /// </summary>
    public partial class ToolboxDanmakuMergerPage
    {
        private static readonly ILogger logger = Log.ForContext<ToolboxDanmakuMergerPage>();
        private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        private readonly ObservableCollection<string> Files = new ObservableCollection<string>();

        public ToolboxDanmakuMergerPage()
        {
            this.InitializeComponent();
            this.listBox.ItemsSource = this.Files;
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            var f = (string)b.DataContext;
            this.Files.Remove(f);
        }

        private void listBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    for (var i = 0; i < files.Length; i++)
                    {
                        var file = files[i];
                        if (file.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.Files.Add(file);
                        }
                    }
                }
            }
            catch (Exception)
            { }
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            var d = new CommonOpenFileDialog
            {
                Title = LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:Toolbox_Merge_OpenFileDialogTitle"),
                AllowNonFileSystemItems = false,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureValidNames = true,
                DefaultDirectory = DesktopPath,
                DefaultExtension = "xml",
                Multiselect = true,
            };

            d.Filters.Add(new CommonFileDialogFilter(LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:Toolbox_Merge_XmlDanmakuFiles"), "*.xml"));

            if (d.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            foreach (var file in d.FileNames)
            {
                this.Files.Add(file);
            }
        }

        private async void Merge_Click(object sender, RoutedEventArgs e)
        {
            AutoFixProgressDialog? progressDialog = null;

            try
            {
                var inputPaths = this.Files.Distinct().ToArray();

                if (inputPaths.Length < 2)
                {
                    MessageBox.Show(LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:Toolbox_Merge_Error_AtLeastTwo"),
                        LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:Toolbox_Merge_Title"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                logger.Debug("合并弹幕文件 {Paths}", inputPaths);

                progressDialog = new AutoFixProgressDialog()
                {
                    CancelButtonVisibility = Visibility.Collapsed,
                    CancellationTokenSource = new CancellationTokenSource()
                };
                var token = progressDialog.CancellationTokenSource.Token;
                var showTask = progressDialog.ShowAsync();

                string? outputPath;
                {
                    var fileDialog = new CommonSaveFileDialog()
                    {
                        Title = LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:Toolbox_AutoFix_SelectOutputDialog_Title"),
                        AlwaysAppendDefaultExtension = true,
                        AddToMostRecentlyUsedList = false,
                        DefaultExtension = "xml",
                        EnsurePathExists = true,
                        EnsureValidNames = true,
                        NavigateToShortcut = true,
                        OverwritePrompt = false,
                        InitialDirectory = Path.GetDirectoryName(inputPaths[0]),
                    };

                    fileDialog.Filters.Add(new CommonFileDialogFilter(LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:Toolbox_Merge_XmlDanmakuFiles"), "*.xml"));

                    if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                        outputPath = fileDialog.FileName;
                    else
                        return;
                }

                var req = new DanmakuMergerRequest
                {
                    Inputs = inputPaths,
                    Output = outputPath,
                };

                var handler = new DanmakuMergerHandler();

                var resp = await handler.Handle(req, token, async p =>
                {
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        progressDialog.Progress = (int)(p * 98d);
                    });
                }).ConfigureAwait(true);

                logger.Debug("弹幕合并结果 {@Response}", resp);

                if (resp.Status != ResponseStatus.Cancelled && resp.Status != ResponseStatus.OK)
                {
                    logger.Warning(resp.Exception, "弹幕合并时发生错误 (@Status)", resp.Status);
                    await Task.Run(() => ShowErrorMessageBox(resp)).ConfigureAwait(true);
                }
                else
                {
                    this.Files.Clear();
                }

                progressDialog.Hide();
                await showTask.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "弹幕合并时发生未处理的错误");
            }
            finally
            {
                try
                {
                    _ = this.Dispatcher.BeginInvoke((Action)(() => progressDialog?.Hide()));
                    progressDialog?.CancellationTokenSource?.Cancel();
                }
                catch (Exception) { }
            }
        }

        private static void ShowErrorMessageBox<T>(CommandResponse<T> resp) where T : IResponseData
        {
            var title = LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:Toolbox_AutoFix_Error_Title");
            var type = LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:Toolbox_AutoFix_Error_Type_" + resp.Status.ToString());
            MessageBox.Show($"{type}\n{resp.ErrorMessage}", title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
