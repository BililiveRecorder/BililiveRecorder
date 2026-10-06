using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BililiveRecorder.Desktop.Infrastructure;
using BililiveRecorder.Desktop.Localization;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    /// <summary>
    /// 首启/路径不可用时的录制工作目录选择窗口。
    /// 确认成功后主窗口已经显示，本窗口再关闭不会触发程序退出。
    /// </summary>
    public partial class SelectWorkDirectoryWindow : Window
    {
        private static readonly ILogger logger = Log.ForContext<SelectWorkDirectoryWindow>();

        private readonly ILocalizationService loc = LocalizationService.Instance;
        private readonly Func<string, bool, StartResult>? start;

        public SelectWorkDirectoryWindow()
        {
            this.InitializeComponent();
        }

        public SelectWorkDirectoryWindow(WindowIcon icon, string prefillPath, StartResult? initialError, Func<string, bool, StartResult> start)
            : this()
        {
            this.start = start;
            this.Icon = icon;
            this.Title = this.loc["WorkDirectorySelector_Title"];
            this.TitleText.Text = this.loc["WorkDirectorySelector_Title"];
            this.BrowseButton.Content = this.loc["WorkDirectorySelector_Browse"];
            this.SkipAskingBox.Content = this.loc["WorkDirectorySelector_SkipAsking"];
            this.QuitButton.Content = this.loc["Global_Quit"];
            this.ConfirmButton.Content = this.loc["Global_Confirm"];
            this.PathBox.Text = prefillPath;

            if (initialError is not null)
                this.ShowError(initialError.Value);
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Browse_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = this.loc["WorkDirectorySelector_Title"],
                });

                if (folders.Count > 0)
                    this.PathBox.Text = folders[0].Path.LocalPath;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error opening folder picker");
            }
        }

        private void Confirm_Click(object? sender, RoutedEventArgs e)
        {
            if (this.start is null)
                return;

            var result = this.start(this.PathBox.Text?.Trim() ?? string.Empty, this.SkipAskingBox.IsChecked == true);

            if (result == StartResult.Success)
            {
                this.Close();
            }
            else
            {
                this.ShowError(result);
            }
        }

        private void Quit_Click(object? sender, RoutedEventArgs e) => this.Close();

        private void ShowError(StartResult result)
        {
            this.ErrorText.Text = result switch
            {
                StartResult.PathDoesNotExist => this.loc["WorkDirectorySelector_Error_PathDoesNotExist"],
                StartResult.PathContainsFiles => this.loc["WorkDirectorySelector_Error_PathContainsFiles"],
                StartResult.FailedToLoadConfig => this.loc["WorkDirectorySelector_Error_FailedToLoadConfig"],
                StartResult.PathNotSupported => this.loc["WorkDirectorySelector_Error_PathNotSupported"],
                StartResult.AlreadyRunning => "该目录已在另一个录播姬进程中运行。\n" +
                    "如果刚才只是关闭了窗口，程序可能仍在托盘中运行——请查看托盘图标；\n" +
                    "也可能是 WPF 版录播姬正在使用同一目录，或存在残留进程（可在任务管理器中结束 BililiveRecorder.Desktop.exe）。\n" +
                    "This directory is already in use by another recorder process.",
                _ => this.loc["WorkDirectorySelector_Error_UnknownError"],
            };
            this.ErrorText.IsVisible = true;
        }
    }
}
