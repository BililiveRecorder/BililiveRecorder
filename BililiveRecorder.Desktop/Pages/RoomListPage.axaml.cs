using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BililiveRecorder.Core;
using BililiveRecorder.Desktop.Controls;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.Pages
{
    public enum SortedBy
    {
        None,
        RoomId,
        Status
    }

    public partial class RoomListPage : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<RoomListPage>();

        public ObservableCollection<IRoom> RoomList { get; } = new ObservableCollection<IRoom>();
        private IRecorder? Recorder => (this.DataContext as Models.RootModel)?.Recorder;
        private bool showLog = false;
        private SortedBy sortedBy = SortedBy.None;

        public RoomListPage()
        {
            this.InitializeComponent();

            this.PropertyChanged += RoomListPage_PropertyChanged;
        }

        private void RoomListPage_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == nameof(DataContext))
            {
                UpdateRoomList();
            }
        }

        private void UpdateRoomList()
        {
            var recorder = Recorder;
            if (recorder != null)
            {
                RoomList.Clear();
                foreach (var room in recorder.Rooms)
                {
                    RoomList.Add(room);
                }
                RefreshRoomCards();
            }
        }

        private void RefreshRoomCards()
        {
            // Clear existing room cards (except AddRoomCard)
            var itemsToRemove = RoomCardsPanel.Children
                .OfType<RoomCard>()
                .ToList();
            
            foreach (var item in itemsToRemove)
            {
                RoomCardsPanel.Children.Remove(item);
            }

            // Add room cards
            var rooms = sortedBy switch
            {
                SortedBy.RoomId => RoomList.OrderBy(r => r.RoomConfig.RoomId).ToList(),
                SortedBy.Status => RoomList.OrderByDescending(r => r.Recording).ThenByDescending(r => r.Streaming).ToList(),
                _ => RoomList.ToList()
            };

            for (int i = 0; i < rooms.Count; i++)
            {
                var roomCard = new RoomCard();
                roomCard.DataContext = rooms[i];
                roomCard.DeleteRequested += RoomCard_DeleteRequested;
                roomCard.ShowSettingsRequested += RoomCard_ShowSettingsRequested;
                RoomCardsPanel.Children.Insert(i, roomCard);
            }
        }

        private void MenuItem_OpenWorkDirectory_Click(object? sender, RoutedEventArgs e)
        {
            var recorder = Recorder;
            if (recorder?.Config.Global.WorkDirectory is string path)
            {
                OpenFolder(path);
            }
        }

        private void MenuItem_SaveConfig_Click(object? sender, RoutedEventArgs e)
        {
            Recorder?.SaveConfig();
        }

        private void MenuItem_ChangeWorkPath_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Implement work path change
            logger.Information("Change work path requested - not yet implemented");
        }

        private void MenuItem_ShowLogFilesInExplorer_Click(object? sender, RoutedEventArgs e)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
            if (Directory.Exists(logPath))
            {
                OpenFolder(logPath);
            }
        }

        private void MenuItem_SortBy_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string tagStr)
            {
                sortedBy = Enum.TryParse<SortedBy>(tagStr, out var sort) ? sort : SortedBy.None;
                RefreshRoomCards();
            }
        }

        private void MenuItem_ToggleNotifyStreamStart_Click(object? sender, RoutedEventArgs e)
        {
            var recorder = Recorder;
            if (recorder != null)
            {
                recorder.Config.Global.WpfNotifyStreamStart = !recorder.Config.Global.WpfNotifyStreamStart;
            }
        }

        private void MenuItem_ToggleShowTitleAndArea_Click(object? sender, RoutedEventArgs e)
        {
            var recorder = Recorder;
            if (recorder != null)
            {
                recorder.Config.Global.WpfShowTitleAndArea = !recorder.Config.Global.WpfShowTitleAndArea;
            }
        }

        private void MenuItem_ToggleShowLog_Click(object? sender, RoutedEventArgs e)
        {
            showLog = !showLog;
            LogElement.IsVisible = showLog;
            Splitter.IsVisible = showLog;
            
            // Find the parent grid and update the row definition
            if (this.Content is Grid grid && grid.RowDefinitions.Count > 3)
            {
                if (showLog)
                {
                    grid.RowDefinitions[3].Height = new GridLength(200);
                }
                else
                {
                    grid.RowDefinitions[3].Height = new GridLength(0);
                }
            }
        }

        private void MenuItem_EnableAutoRecAll_Click(object? sender, RoutedEventArgs e)
        {
            var recorder = Recorder;
            if (recorder != null)
            {
                foreach (var room in recorder.Rooms)
                {
                    room.RoomConfig.AutoRecord = true;
                }
            }
        }

        private void MenuItem_DisableAutoRecAll_Click(object? sender, RoutedEventArgs e)
        {
            var recorder = Recorder;
            if (recorder != null)
            {
                foreach (var room in recorder.Rooms)
                {
                    room.RoomConfig.AutoRecord = false;
                }
            }
        }

        private async void MenuItem_RefreshAllRoomInfo_Click(object? sender, RoutedEventArgs e)
        {
            var recorder = Recorder;
            if (recorder != null)
            {
                foreach (var room in recorder.Rooms)
                {
                    try
                    {
                        await room.RefreshRoomInfoAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.Warning(ex, "Failed to refresh room info for room {RoomId}", room.RoomConfig.RoomId);
                    }
                }
            }
        }

        private void MenuItem_OpenWebsite_Click(object? sender, RoutedEventArgs e) => OpenUrl("https://rec.danmuji.org/");
        private void MenuItem_OpenFAQ_Click(object? sender, RoutedEventArgs e) => OpenUrl("https://rec.danmuji.org/link/faq/");
        private void MenuItem_OpenSponsor_Click(object? sender, RoutedEventArgs e) => OpenUrl("https://rec.danmuji.org/link/sponsor/");

        private void AddRoomCard_AddRoomRequested(object? sender, AddRoomCard.AddRoomRequestedEventArgs e)
        {
            var recorder = Recorder;
            if (recorder == null) return;

            try
            {
                var room = recorder.AddRoom(e.RoomId, e.AutoRecord);
                if (room != null)
                {
                    RoomList.Add(room);
                    RefreshRoomCards();
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to add room {RoomId}", e.RoomId);
                // TODO: Show error dialog
            }
        }

        private async void RoomCard_DeleteRequested(object? sender, RoomCard.DeleteRequestedEventArgs e)
        {
            var recorder = Recorder;
            if (recorder == null) return;

            try
            {
                // Show confirmation dialog
                var dialog = new DeleteRoomConfirmDialog
                {
                    Room = e.Room
                };
                var result = await dialog.ShowAsync();
                if (result == FluentAvalonia.UI.Controls.ContentDialogResult.Primary)
                {
                    recorder.RemoveRoom(e.Room);
                    RoomList.Remove(e.Room);
                    RefreshRoomCards();
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to delete room");
            }
        }

        private async void RoomCard_ShowSettingsRequested(object? sender, RoomCard.ShowSettingsRequestedEventArgs e)
        {
            try
            {
                var dialog = new PerRoomSettingsDialog
                {
                    Room = e.Room
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to show room settings");
            }
        }

        private static void OpenUrl(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            }
            catch (Exception) { }
        }

        private static void OpenFolder(string path)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start("explorer.exe", path);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", path);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", path);
                }
            }
            catch (Exception) { }
        }
    }
}
