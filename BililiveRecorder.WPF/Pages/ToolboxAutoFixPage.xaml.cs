using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using BililiveRecorder.ToolBox;
using BililiveRecorder.ToolBox.Commands;
using BililiveRecorder.WPF.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;

#nullable enable
namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for ToolboxAutoFixPage.xaml
    /// </summary>
    public partial class ToolboxAutoFixPage
    {
        private static readonly ILogger logger = Log.ForContext<ToolboxAutoFixPage>();

        public ToolboxAutoFixPage()
        {
            this.InitializeComponent();
        }

        private void SelectFile_Button_Click(object sender, RoutedEventArgs e)
        {
            // var title = LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:WorkDirectorySelector_Title");
            var title = "选择要修复的文件";
            var fileDialog = new CommonOpenFileDialog()
            {
                Title = title,
                IsFolderPicker = false,
                Multiselect = false,
                AllowNonFileSystemItems = false,
                AddToMostRecentlyUsedList = false,
                EnsurePathExists = true,
                EnsureFileExists = true,
                NavigateToShortcut = true,
                Filters =
                {
                    new CommonFileDialogFilter("Flv files",".flv"),
                    new CommonFileDialogFilter("Flv Xml files",".xml,.gz")
                }
            };
            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.FileNameTextBox.Text = fileDialog.FileName;
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Fix_Button_Click(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            AutoFixProgressDialog? progressDialog = null;
            try
            {
                var inputPath = this.FileNameTextBox.Text;
                if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
                    return;

                logger.Debug("修复文件 {Path}", inputPath);

                progressDialog = new AutoFixProgressDialog();
                var showTask = progressDialog.ShowAsync();

                string? output_path;
                {
                    var title = "选择保存位置";
                    var fileDialog = new CommonSaveFileDialog()
                    {
                        Title = title,
                        AddToMostRecentlyUsedList = false,
                        EnsurePathExists = true,
                        EnsureValidNames = true,
                        NavigateToShortcut = true,
                        OverwritePrompt = false,
                        InitialDirectory = Path.GetDirectoryName(inputPath),
                        DefaultDirectory = Path.GetDirectoryName(inputPath),
                        DefaultFileName = Path.GetFileName(inputPath)
                    };
                    if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                        output_path = fileDialog.FileName;
                    else
                        return;
                }

                var req = new FixRequest
                {
                    Input = inputPath,
                    OutputBase = output_path,
                };

                var handler = new FixHandler();

                var resp = await handler.Handle(req, async p =>
                {
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        progressDialog.Progress = (int)(p * 98d);
                    });
                }).ConfigureAwait(true);

                if (resp.Status != ResponseStatus.OK)
                {
                    logger.Warning(resp.Exception, "修复时发生错误 (@Status)", resp.Status);
                    await Task.Run(() =>
                    {
                        // TODO 翻译
                        // 例：在读取文件时发生了错误
                        // 选择的不是 FLV 文件
                        // FLV 文件格式错误
                        MessageBox.Show($"错误类型: {resp.Status}\n{resp.ErrorMessage}", "修复时发生错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }).ConfigureAwait(true);
                }

                progressDialog.Hide();
                await showTask.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "修复时发生未处理的错误");
            }
            finally
            {
                try
                {
                    _ = this.Dispatcher.BeginInvoke((Action)(() => progressDialog?.Hide()));
                }
                catch (Exception) { }
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Analyze_Button_Click(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            AutoFixProgressDialog? progressDialog = null;
            try
            {
                var inputPath = this.FileNameTextBox.Text;
                if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
                    return;

                logger.Debug("分析文件 {Path}", inputPath);

                progressDialog = new AutoFixProgressDialog();
                var showTask = progressDialog.ShowAsync();

                var req = new AnalyzeRequest
                {
                    Input = inputPath
                };

                var handler = new AnalyzeHandler();

                var resp = await handler.Handle(req, async p =>
                {
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        progressDialog.Progress = (int)(p * 98d);
                    });
                }).ConfigureAwait(true);

                if (resp.Status != ResponseStatus.OK)
                {
                    logger.Warning(resp.Exception, "分析时发生错误 (@Status)", resp.Status);
                    await Task.Run(() =>
                    {
                        // TODO 翻译
                        // 例：在读取文件时发生了错误
                        // 选择的不是 FLV 文件
                        // FLV 文件格式错误
                        MessageBox.Show($"错误类型: {resp.Status}\n{resp.ErrorMessage}", "分析时发生错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }).ConfigureAwait(true);
                }
                else
                {
                    this.analyzeResultDisplayArea.DataContext = resp.Result;
                }

                progressDialog.Hide();
                await showTask.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "分析时发生未处理的错误");
            }
            finally
            {
                try
                {
                    _ = this.Dispatcher.BeginInvoke((Action)(() => progressDialog?.Hide()));
                }
                catch (Exception) { }
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Export_Button_Click(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            AutoFixProgressDialog? progressDialog = null;
            try
            {
                var inputPath = this.FileNameTextBox.Text;
                if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
                    return;

                logger.Debug("导出文件 {Path}", inputPath);

                progressDialog = new AutoFixProgressDialog();
                var showTask = progressDialog.ShowAsync();

                var outputPath = string.Empty;
                {
                    var title = "选择保存位置";
                    var fileDialog = new CommonSaveFileDialog()
                    {
                        Title = title,
                        AddToMostRecentlyUsedList = false,
                        EnsurePathExists = true,
                        EnsureValidNames = true,
                        NavigateToShortcut = true,
                        OverwritePrompt = false,
                        InitialDirectory = Path.GetDirectoryName(inputPath),
                        DefaultDirectory = Path.GetDirectoryName(inputPath),
                        DefaultFileName = Path.GetFileNameWithoutExtension(inputPath) + ".brec.xml.gz"
                    };
                    if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
                        outputPath = fileDialog.FileName;
                    else
                        return;
                }
                if (File.Exists(outputPath))
                {
                    MessageBox.Show("保存位置已经存在文件");
                    return;
                }

                var req = new ExportRequest
                {
                    Input = inputPath,
                    Output = outputPath
                };

                var handler = new ExportHandler();

                var resp = await handler.Handle(req, async p =>
                {
                    await this.Dispatcher.InvokeAsync(() =>
                    {
                        progressDialog.Progress = (int)(p * 95d);
                    });
                }).ConfigureAwait(true);

                if (resp.Status != ResponseStatus.OK)
                {
                    logger.Warning(resp.Exception, "导出分析数据时发生错误 (@Status)", resp.Status);
                    await Task.Run(() =>
                    {
                        // TODO 翻译
                        // 例：在读取文件时发生了错误
                        // 选择的不是 FLV 文件
                        // FLV 文件格式错误
                        MessageBox.Show($"错误类型: {resp.Status}\n{resp.ErrorMessage}", "导出分析数据时发生错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }).ConfigureAwait(true);
                }

                progressDialog.Hide();
                await showTask.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "导出时发生未处理的错误");
            }
            finally
            {
                try
                {
                    _ = this.Dispatcher.BeginInvoke((Action)(() => progressDialog?.Hide()));
                }
                catch (Exception) { }
            }
        }

        private void FileNameTextBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    this.FileNameTextBox.Text = files[0];
                }
            }
            catch (Exception)
            { }
        }
    }
}
