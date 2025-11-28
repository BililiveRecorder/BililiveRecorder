using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BililiveRecorder.Core;

#nullable enable
namespace BililiveRecorder.Desktop.Controls
{
    public partial class RoomCard : UserControl
    {
        public event EventHandler<DeleteRequestedEventArgs>? DeleteRequested;
        public event EventHandler<ShowSettingsRequestedEventArgs>? ShowSettingsRequested;

        private IRoom? Room => DataContext as IRoom;

        public RoomCard()
        {
            this.InitializeComponent();
        }

        private void MenuButton_Click(object? sender, RoutedEventArgs e)
        {
            // Menu opens via Flyout
        }

        private void MenuItem_StartRecording_Click(object? sender, RoutedEventArgs e)
        {
            Room?.StartRecord();
        }

        private void MenuItem_StopRecording_Click(object? sender, RoutedEventArgs e)
        {
            Room?.StopRecord();
        }

        private async void MenuItem_RefreshInfo_Click(object? sender, RoutedEventArgs e)
        {
            var room = Room;
            if (room != null)
            {
                try
                {
                    await room.RefreshRoomInfoAsync();
                }
                catch (Exception) { }
            }
        }

        private void MenuItem_OpenInBrowser_Click(object? sender, RoutedEventArgs e)
        {
            var room = Room;
            if (room != null)
            {
                var url = $"https://live.bilibili.com/{room.RoomConfig.RoomId}";
                OpenUrl(url);
            }
        }

        private void MenuItem_ShowSettings_Click(object? sender, RoutedEventArgs e)
        {
            var room = Room;
            if (room != null)
            {
                ShowSettingsRequested?.Invoke(this, new ShowSettingsRequestedEventArgs(room));
            }
        }

        private void MenuItem_EnableAutoRecord_Click(object? sender, RoutedEventArgs e)
        {
            var room = Room;
            if (room != null)
            {
                room.RoomConfig.AutoRecord = true;
            }
        }

        private void MenuItem_DisableAutoRecord_Click(object? sender, RoutedEventArgs e)
        {
            var room = Room;
            if (room != null)
            {
                room.RoomConfig.AutoRecord = false;
            }
        }

        private void MenuItem_DeleteRoom_Click(object? sender, RoutedEventArgs e)
        {
            var room = Room;
            if (room != null)
            {
                DeleteRequested?.Invoke(this, new DeleteRequestedEventArgs(room));
            }
        }

        private void Button_Split_Click(object? sender, RoutedEventArgs e)
        {
            Room?.SplitOutput();
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

        public class DeleteRequestedEventArgs : EventArgs
        {
            public IRoom Room { get; }

            public DeleteRequestedEventArgs(IRoom room)
            {
                Room = room;
            }
        }

        public class ShowSettingsRequestedEventArgs : EventArgs
        {
            public IRoom Room { get; }

            public ShowSettingsRequestedEventArgs(IRoom room)
            {
                Room = room;
            }
        }
    }
}
