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
        public event EventHandler<IRoom>? DeleteRequested;
        public event EventHandler<IRoom>? ShowSettingsRequested;
        public event EventHandler? ShowGlobalSettingsRequested;

        public RoomCard()
        {
            this.InitializeComponent();
        }

        private IRoom? Room => this.DataContext as IRoom;

        private void MenuItem_StartRecording_Click(object? sender, RoutedEventArgs e)
        {
            this.Room?.StartRecord();
        }

        private void MenuItem_StopRecording_Click(object? sender, RoutedEventArgs e)
        {
            this.Room?.StopRecord();
        }

        private void MenuItem_RefreshInfo_Click(object? sender, RoutedEventArgs e)
        {
            _ = this.Room?.RefreshRoomInfoAsync();
        }

        private void MenuItem_OpenInBrowser_Click(object? sender, RoutedEventArgs e)
        {
            var room = this.Room;
            if (room != null)
            {
                var url = $"https://live.bilibili.com/{room.RoomConfig.RoomId}";
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
        }

        private void MenuItem_ShowSettings_Click(object? sender, RoutedEventArgs e)
        {
            var room = this.Room;
            if (room != null)
            {
                ShowSettingsRequested?.Invoke(this, room);
            }
        }

        private void MenuItem_ShowGlobalSettings_Click(object? sender, RoutedEventArgs e)
        {
            ShowGlobalSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void MenuItem_StartMonitor_Click(object? sender, RoutedEventArgs e)
        {
            var room = this.Room;
            if (room != null)
            {
                room.RoomConfig.AutoRecord = true;
            }
        }

        private void MenuItem_StopMonitor_Click(object? sender, RoutedEventArgs e)
        {
            var room = this.Room;
            if (room != null)
            {
                room.RoomConfig.AutoRecord = false;
            }
        }

        private void MenuItem_DeleteRoom_Click(object? sender, RoutedEventArgs e)
        {
            var room = this.Room;
            if (room != null)
            {
                DeleteRequested?.Invoke(this, room);
            }
        }

        private void Button_Split_Click(object? sender, RoutedEventArgs e)
        {
            this.Room?.SplitOutput();
        }
    }
}
