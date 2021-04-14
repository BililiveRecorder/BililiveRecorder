using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Writer;
using BililiveRecorder.Flv.Xml;
using BililiveRecorder.ToolBox.Commands;
using BililiveRecorder.WPF.Controls;
using BililiveRecorder.WPF.Models;
using Microsoft.Extensions.DependencyInjection;
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
                    new CommonFileDialogFilter("Flv files",".flv")
                }
            };
            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                this.FileNameTextBox.Text = fileDialog.FileName;
            }
        }

        private async void Fix_Button_Click(object sender, RoutedEventArgs e)
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

                progressDialog.Hide();
                await showTask.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "修复时发生错误");
                MessageBox.Show("修复时发生错误\n" + ex.Message);
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

        private async void Analyze_Button_Click(object sender, RoutedEventArgs e)
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

                this.analyzeResultDisplayArea.DataContext = resp;

                progressDialog.Hide();
                await showTask.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "分析时发生错误");
                MessageBox.Show("分析时发生错误\n" + ex.Message);
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

        private async void Export_Button_Click(object sender, RoutedEventArgs e)
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

                progressDialog.Hide();
                await showTask.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "导出时发生错误");
                MessageBox.Show("导出时发生错误\n" + ex.Message);
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
    }
}
