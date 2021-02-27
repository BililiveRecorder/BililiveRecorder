using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Windows;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Writer;
using BililiveRecorder.Flv.Xml;
using BililiveRecorder.WPF.Controls;
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

                IFlvWriterTargetProvider? targetProvider = null;
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
                        targetProvider = new AutoFixFlvWriterTargetProvider(fileDialog.FileName);
                    else
                        return;
                }

                using var inputStream = File.OpenRead(inputPath);
                var memoryStreamProvider = new DefaultMemoryStreamProvider();
                using var grouping = new TagGroupReader(new FlvTagPipeReader(PipeReader.Create(inputStream), memoryStreamProvider, skipData: false, logger: logger));
                using var writer = new FlvProcessingContextWriter(targetProvider, memoryStreamProvider, logger);
                var context = new FlvProcessingContext();
                var session = new Dictionary<object, object?>();
                var pipeline = new ProcessingPipelineBuilder(new ServiceCollection().BuildServiceProvider()).AddDefault().AddRemoveFillerData().Build();

                await Task.Run(async () =>
                {
                    var count = 0;
                    while (true)
                    {
                        var group = await grouping.ReadGroupAsync(default).ConfigureAwait(false);
                        if (group is null)
                            break;

                        context.Reset(group, session);
                        await pipeline(context).ConfigureAwait(false);

                        if (context.Comments.Count > 0)
                            logger.Debug("修复逻辑输出 {Comments}", string.Join("\n", context.Comments));

                        await writer.WriteAsync(context).ConfigureAwait(false);

                        foreach (var action in context.Output)
                            if (action is PipelineDataAction dataAction)
                                foreach (var tag in dataAction.Tags)
                                    tag.BinaryData?.Dispose();

                        if (count++ % 5 == 0)
                        {
                            await this.Dispatcher.InvokeAsync(() =>
                            {
                                progressDialog.Progress = (int)((double)inputStream.Position / inputStream.Length * 98d);
                            });
                        }
                    }
                }).ConfigureAwait(true);

                progressDialog.Hide();
                await showTask.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "修复时发生错误");
                throw;
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

        private void Analyze_Button_Click(object sender, RoutedEventArgs e)
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


            }
            catch (Exception ex)
            {
                logger.Warning(ex, "分析时发生错误");
                throw;
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

                using var inputStream = File.OpenRead(inputPath);
                var outputStream = File.OpenWrite(outputPath);

                var tags = new List<Tag>();
                using var reader = new FlvTagPipeReader(PipeReader.Create(inputStream), new DefaultMemoryStreamProvider(), skipData: true, logger: logger);
                await Task.Run(async () =>
                {
                    var count = 0;
                    while (true)
                    {
                        var tag = await reader.ReadTagAsync(default).ConfigureAwait(false);
                        if (tag is null) break;
                        tags.Add(tag);
                        if (count++ % 300 == 0)
                        {
                            await this.Dispatcher.InvokeAsync(() =>
                            {
                                progressDialog.Progress = (int)((double)inputStream.Position / inputStream.Length * 98d);
                            });
                        }
                    }
                }).ConfigureAwait(true);

                using (var writer = new StreamWriter(new GZipStream(outputStream, CompressionLevel.Optimal)))
                    XmlFlvFile.Serializer.Serialize(writer, new XmlFlvFile
                    {
                        Tags = tags
                    });

                progressDialog.Hide();
                await showTask.ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "导出时发生错误");
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

        private class AutoFixFlvWriterTargetProvider : IFlvWriterTargetProvider
        {
            private readonly string pathTemplate;
            private int fileIndex = 1;

            public AutoFixFlvWriterTargetProvider(string pathTemplate)
            {
                this.pathTemplate = pathTemplate;
            }

            public Stream CreateAlternativeHeaderStream()
            {
                var path = Path.ChangeExtension(this.pathTemplate, "header.txt");
                return File.Open(path, FileMode.Append, FileAccess.Write, FileShare.None);
            }

            public (Stream stream, object state) CreateOutputStream()
            {
                var i = this.fileIndex++;
                var path = Path.ChangeExtension(this.pathTemplate, $"fix_p{i}.flv");
                var fileStream = File.Create(path);
                return (fileStream, null!);
            }

            public bool ShouldCreateNewFile(Stream outputStream, IList<Tag> tags) => false;
        }
    }
}
