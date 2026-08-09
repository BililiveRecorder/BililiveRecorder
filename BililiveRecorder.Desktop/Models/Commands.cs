using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

#nullable enable
namespace BililiveRecorder.Desktop.Models
{
    internal static class Commands
    {
        public static ICommand OpenLink { get; } = new OpenLinkCommand();
        public static ICommand Copy { get; } = new CopyCommand();

        private class OpenLinkCommand : ICommand
        {
            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter)
            {
                if (parameter is string url)
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
                    catch (Exception)
                    {
                        // Ignore
                    }
                }
            }
        }

        private class CopyCommand : ICommand
        {
            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => true;

            public async void Execute(object? parameter)
            {
                if (parameter is string text)
                {
                    try
                    {
                        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                            && desktop.MainWindow?.Clipboard is { } clipboard)
                        {
                            await clipboard.SetTextAsync(text);
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore
                    }
                }
            }
        }
    }
}
