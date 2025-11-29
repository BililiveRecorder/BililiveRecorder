using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BililiveRecorder.Core;
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
        private IRecorder? recorder;

        public RoomListPage()
        {
            this.InitializeComponent();
        }

        private void MenuItem_OpenWorkDirectory_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var path = recorder?.Config?.Global?.WorkDirectory;
                if (!string.IsNullOrEmpty(path))
                {
                    OpenFolder(path);
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to open work directory");
            }
        }

        private void MenuItem_SaveConfig_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                recorder?.SaveConfig();
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to save config");
            }
        }

        private void MenuItem_ChangeWorkPath_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Implement change work path dialog
        }

        private void MenuItem_ShowLogFilesInExplorer_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var path = System.IO.Path.Combine(AppContext.BaseDirectory, "logs");
                OpenFolder(path);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to show log files");
            }
        }

        private void MenuItem_SortBy_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Implement sorting
        }

        private void MenuItem_ShowLog_Click(object? sender, RoutedEventArgs e)
        {
            this.LogElement.IsVisible = !this.LogElement.IsVisible;
        }

        private void MenuItem_EnableAutoRecAll_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Enable auto record for all rooms
        }

        private void MenuItem_DisableAutoRecAll_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Disable auto record for all rooms
        }

        private void MenuItem_RefreshAllRoomInfo_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Refresh all room info
        }

        private void AddRoomCard_AddRoomRequested(object? sender, string roomId)
        {
            // TODO: Add room
        }

        private static void OpenFolder(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
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
    }
}
