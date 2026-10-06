using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CliWrap;
using CliWrap.Buffered;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    public partial class ToolboxRemuxView : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<ToolboxRemuxView>();

        /// <summary>与 WPF 版相同的 FFmpeg 存放约定：exe 旁 lib/miniffmpeg。</summary>
        private static readonly string ffmpegWorkingDirectory =
            Path.Combine(AppContext.BaseDirectory, "lib");

        private static string FFmpegPath => Path.Combine(ffmpegWorkingDirectory, "miniffmpeg");

        private bool running;

        public ToolboxRemuxView()
        {
            this.InitializeComponent();
            this.DataContext = new ViewModels.ToolboxViewModel();
        }

        private BililiveRecorder.Desktop.Localization.ILocalizationService Loc =>
            BililiveRecorder.Desktop.Localization.LocalizationService.Instance;

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void BrowseInput_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null)
                    return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = this.Loc["Toolbox_Remux_OpenFileTitle"],
                    FileTypeFilter = new[] { new FilePickerFileType("FLV") { Patterns = new[] { "*.flv" } } },
                });
                if (files.Count > 0)
                    this.InputPathBox.Text = files[0].Path.LocalPath;
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error picking input file");
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Remux_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            var input = this.InputPathBox.Text?.Trim();
            if (string.IsNullOrEmpty(input) || this.running)
                return;

            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is null)
                    return;

                if (!File.Exists(FFmpegPath))
                {
                    this.ShowStatus($"未找到 FFmpeg: {FFmpegPath}\n请将 FFmpeg 放到程序目录的 lib 文件夹并命名为 miniffmpeg。\nFFmpeg not found; place it at lib/miniffmpeg next to the executable.");
                    return;
                }

                var output = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    SuggestedFileName = Path.GetFileNameWithoutExtension(input),
                    FileTypeChoices = new[] { new FilePickerFileType("MP4") { Patterns = new[] { "*.mp4" } } },
                });
                var outputPath = output?.Path.LocalPath;
                if (string.IsNullOrEmpty(outputPath))
                    return;

                this.running = true;
                this.RemuxButton.IsEnabled = false;
                this.ShowStatus("…");

                var result = await Cli.Wrap(FFmpegPath)
                    .WithValidation(CliWrap.CommandResultValidation.None)
                    .WithWorkingDirectory(ffmpegWorkingDirectory)
                    .WithArguments(new[] { "-hide_banner", "-loglevel", "error", "-y", "-i", input, "-c", "copy", outputPath })
                    .ExecuteBufferedAsync();

                this.ShowStatus(result.ExitCode == 0
                    ? "✓ " + outputPath
                    : $"FFmpeg exit code: {result.ExitCode}\n{result.StandardError}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error remuxing file");
                this.ShowStatus("Error: " + ex.Message);
            }
            finally
            {
                this.running = false;
                await Dispatcher.UIThread.InvokeAsync(() => this.RemuxButton.IsEnabled = true);
            }
        }

        private void ShowStatus(string text)
        {
            Dispatcher.UIThread.Post(() => this.StatusText.Text = text);
        }
    }
}
