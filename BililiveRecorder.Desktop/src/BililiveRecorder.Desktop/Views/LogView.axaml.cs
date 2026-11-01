using System;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
using BililiveRecorder.Desktop.ViewModels;

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    public partial class LogView : UserControl
    {
        public LogView()
        {
            this.InitializeComponent();
            this.DataContext = new LogViewModel();

            if (this.LogList.Items is INotifyCollectionChanged changed)
                changed.CollectionChanged += this.Logs_CollectionChanged;
        }

        private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 鼠标悬停在列表上时不自动滚动，方便查看
            if (this.LogList.IsPointerOver)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var items = this.LogList.Items;
                    if (items.Count > 0 && items[^1] is { } last)
                        this.LogList.ScrollIntoView(last);
                }
                catch (Exception) { }
            });
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void Copy_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                if (this.LogList.SelectedItem is Infrastructure.LogEntry entry)
                {
                    var topLevel = App.MainWindowOrNull;
                    if (topLevel?.Clipboard is not null)
                        await topLevel.Clipboard.SetTextAsync($"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] [{entry.RoomId}] {entry.Message}");
                }
            }
            catch (Exception) { }
        }
    }
}
