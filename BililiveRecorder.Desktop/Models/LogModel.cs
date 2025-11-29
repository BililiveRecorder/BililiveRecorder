using System;
using System.Collections.ObjectModel;
using Avalonia.Threading;

#nullable enable
namespace BililiveRecorder.Desktop.Models
{
    public class LogModel
    {
        public static LogModel Instance { get; } = new LogModel();

        public ObservableCollection<LogEntry> Logs { get; } = new ObservableCollection<LogEntry>();

        public void AddLog(LogEntry entry)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Logs.Add(entry);
                while (Logs.Count > 1000)
                    Logs.RemoveAt(0);
            });
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public int? RoomId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
