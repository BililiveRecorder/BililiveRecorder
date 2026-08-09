using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;

#nullable enable
namespace BililiveRecorder.Desktop.Controls
{
    public class WorkDirectorySelectorDialog : ContentDialog
    {
        public enum WorkDirectorySelectorDialogError
        {
            None,
            PathDoesNotExist,
            PathNotSupported,
            PathContainsFiles,
            FailedToLoadConfig,
            UnknownError
        }

        private TextBox? _pathTextBox;
        private CheckBox? _skipAskingCheckBox;
        private TextBlock? _errorTextBlock;

        public WorkDirectorySelectorDialogError Error { get; set; } = WorkDirectorySelectorDialogError.None;
        public string Path { get; set; } = "";
        public bool SkipAsking { get; set; } = false;

        public WorkDirectorySelectorDialog()
        {
            Title = "选择工作目录 Select Work Directory";
            PrimaryButtonText = "确定 OK";
            SecondaryButtonText = "工具箱模式 Toolbox Mode";
            CloseButtonText = "退出 Exit";
            DefaultButton = ContentDialogButton.Primary;
        }

        public new async Task<ContentDialogResult> ShowAsync()
        {
            // Create dialog content
            var content = new StackPanel
            {
                Spacing = 10,
                MinWidth = 500
            };

            content.Children.Add(new TextBlock
            {
                Text = "请选择或创建一个工作目录来存储录制文件和配置。",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });

            content.Children.Add(new TextBlock
            {
                Text = "Please select or create a working directory for recordings and config.",
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });

            // Error message
            _errorTextBlock = new TextBlock
            {
                Foreground = Avalonia.Media.Brushes.Red,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                IsVisible = Error != WorkDirectorySelectorDialogError.None
            };
            UpdateErrorText();
            content.Children.Add(_errorTextBlock);

            // Path input
            var pathPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 5
            };

            _pathTextBox = new TextBox
            {
                Width = 400,
                Text = Path,
                Watermark = "路径 Path"
            };
            pathPanel.Children.Add(_pathTextBox);

            var browseButton = new Button
            {
                Content = "浏览 Browse"
            };
            browseButton.Click += BrowseButton_Click;
            pathPanel.Children.Add(browseButton);

            content.Children.Add(pathPanel);

            // Skip asking checkbox
            _skipAskingCheckBox = new CheckBox
            {
                Content = "不再询问 Don't ask again",
                IsChecked = SkipAsking
            };
            content.Children.Add(_skipAskingCheckBox);

            Content = content;

            var result = await base.ShowAsync();

            // Update properties after dialog closes
            if (_pathTextBox != null)
            {
                Path = _pathTextBox.Text ?? "";
            }
            if (_skipAskingCheckBox != null)
            {
                SkipAsking = _skipAskingCheckBox.IsChecked ?? false;
            }

            return result;
        }

        private void UpdateErrorText()
        {
            if (_errorTextBlock == null) return;

            _errorTextBlock.Text = Error switch
            {
                WorkDirectorySelectorDialogError.PathDoesNotExist => "路径不存在 Path does not exist",
                WorkDirectorySelectorDialogError.PathNotSupported => "路径格式不支持 Path format not supported",
                WorkDirectorySelectorDialogError.PathContainsFiles => "目录不为空且不包含配置文件 Directory is not empty and does not contain config file",
                WorkDirectorySelectorDialogError.FailedToLoadConfig => "加载配置文件失败 Failed to load config file",
                WorkDirectorySelectorDialogError.UnknownError => "未知错误 Unknown error",
                _ => ""
            };
            _errorTextBlock.IsVisible = Error != WorkDirectorySelectorDialogError.None;
        }

        private async void BrowseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                // Get the top-level window to show the folder picker
                var topLevel = TopLevel.GetTopLevel(sender as Visual);
                if (topLevel == null) return;

                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                {
                    Title = "选择工作目录 Select Work Directory",
                    AllowMultiple = false
                });

                if (folders.Count > 0)
                {
                    var path = folders[0].TryGetLocalPath();
                    if (!string.IsNullOrEmpty(path) && _pathTextBox != null)
                    {
                        _pathTextBox.Text = path;
                    }
                }
            }
            catch (Exception)
            {
                // Failed to open folder picker
            }
        }
    }
}
