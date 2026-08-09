using System.Threading.Tasks;
using BililiveRecorder.Core;
using FluentAvalonia.UI.Controls;

#nullable enable
namespace BililiveRecorder.Desktop.Controls
{
    public class PerRoomSettingsDialog : ContentDialog
    {
        public IRoom? Room { get; set; }

        public PerRoomSettingsDialog()
        {
            Title = "房间设置 Room Settings";
            CloseButtonText = "关闭 Close";
            DefaultButton = ContentDialogButton.Close;
        }

        public new async Task<ContentDialogResult> ShowAsync()
        {
            if (Room != null)
            {
                // Create settings content
                var content = new Avalonia.Controls.StackPanel
                {
                    Spacing = 10,
                    MinWidth = 400
                };

                content.Children.Add(new Avalonia.Controls.TextBlock
                {
                    Text = $"房间名: {Room.Name}",
                    FontSize = 16
                });

                content.Children.Add(new Avalonia.Controls.TextBlock
                {
                    Text = $"房间ID: {Room.RoomConfig.RoomId}"
                });

                var autoRecordToggle = new Avalonia.Controls.ToggleSwitch
                {
                    OnContent = "自动录制 Auto Record",
                    OffContent = "自动录制 Auto Record",
                    IsChecked = Room.RoomConfig.AutoRecord
                };
                autoRecordToggle.IsCheckedChanged += (s, e) =>
                {
                    Room.RoomConfig.AutoRecord = autoRecordToggle.IsChecked ?? false;
                };
                content.Children.Add(autoRecordToggle);

                // TODO: Add more per-room settings
                content.Children.Add(new Avalonia.Controls.TextBlock
                {
                    Text = "更多设置待实现... More settings to be implemented...",
                    Foreground = Avalonia.Media.Brushes.Gray,
                    Margin = new Avalonia.Thickness(0, 10, 0, 0)
                });

                Content = content;
            }
            else
            {
                Content = "No room selected";
            }

            return await base.ShowAsync();
        }
    }
}
