using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.WindowsAPICodePack.Dialogs;
using Serilog;
using WPFLocalizeExtension.Extensions;

namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for ToolboxRemuxPage.xaml
    /// </summary>
    public partial class ToolboxRemuxPage
    {
        private static readonly ILogger logger = Log.ForContext<ToolboxRemuxPage>();
        private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private static readonly string FFmpegWorkingDirectory;
        private static readonly string FFmpegPath;

        static ToolboxRemuxPage()
        {
            FFmpegWorkingDirectory = Path.Combine(Path.GetDirectoryName(typeof(ToolboxRemuxPage).Assembly.Location), "lib");
            FFmpegPath = Path.Combine(FFmpegWorkingDirectory, "miniffmpeg");
        }

        public ToolboxRemuxPage()
        {
            this.InitializeComponent();
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void RemuxButton_Click(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                await this.RunAsync();
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "转封装时发生未知错误");
            }
        }

        private async Task RunAsync()
        {
            string source, target;

            {

                var d = new CommonOpenFileDialog()
                {
                    Title = LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:Toolbox_Remux_OpenFileTitle"),
                    AllowNonFileSystemItems = false,
                    DefaultDirectory = DesktopPath,
                    DefaultExtension = "flv",
                    EnsureFileExists = true,
                    EnsurePathExists = true,
                    EnsureValidNames = true,
                    Multiselect = false,
                };

                d.Filters.Add(new CommonFileDialogFilter("FLV", "*.flv"));

                if (d.ShowDialog() != CommonFileDialogResult.Ok)
                    return;

                source = d.FileName;
            }

            {
                var d = new CommonSaveFileDialog()
                {
                    Title = LocExtension.GetLocalizedValue<string>("BililiveRecorder.WPF:Strings:Toolbox_Remux_SaveFileTitle"),
                    AlwaysAppendDefaultExtension = true,
                    DefaultDirectory = DesktopPath,
                    DefaultExtension = "mp4",
                    EnsurePathExists = true,
                    EnsureValidNames = true,
                    InitialDirectory = Path.GetDirectoryName(source),
                    DefaultFileName = Path.GetFileNameWithoutExtension(source),
                };

                d.Filters.Add(new CommonFileDialogFilter("MP4", "*.mp4"));

                if (d.ShowDialog() != CommonFileDialogResult.Ok)
                    return;

                target = d.FileName;
            }

            logger.Debug("Remux starting, {Source}, {Target}", source, target);

            var result = await Cli.Wrap(FFmpegPath)
                .WithValidation(CommandResultValidation.None)
                .WithWorkingDirectory(FFmpegWorkingDirectory)
                .WithArguments(new[] { "-hide_banner", "-loglevel", "error", "-y", "-i", source, "-c", "copy", target })
#if DEBUG
                .ExecuteBufferedAsync();
#else
                .ExecuteAsync();
#endif

            logger.Debug("Remux completed {@Result}", result);
        }
    }
}
