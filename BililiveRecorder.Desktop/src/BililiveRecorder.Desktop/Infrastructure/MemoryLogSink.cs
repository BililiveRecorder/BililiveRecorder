using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Threading;
using Serilog.Core;
using Serilog.Events;

#nullable enable
namespace BililiveRecorder.Desktop.Infrastructure
{
    /// <summary>
    /// 保留最近日志在内存中供日志页显示，与 WPF 版 WpfLogEventSink 相同的思路。
    /// </summary>
    public class MemoryLogSink : ILogEventSink
    {
        private const int MaxLines = 500;
        private static readonly object gate = new object();

        public static ObservableCollection<LogEntry> Logs { get; } = new ObservableCollection<LogEntry>();

        public void Emit(LogEvent logEvent)
        {
            var msg = logEvent.RenderMessage();
            if (logEvent.Exception is not null)
                msg += " " + logEvent.Exception.Message;

            var entry = new LogEntry
            {
                Timestamp = logEvent.Timestamp,
                Level = logEvent.Level,
                Message = msg,
            };

            if (logEvent.Properties.TryGetValue("RoomId", out var propertyValue)
                && propertyValue is ScalarValue scalarValue
                && scalarValue.Value is int roomid)
            {
                entry.RoomId = roomid.ToString();
            }

            var current = Application.Current;
            if (current is null)
            {
                lock (gate)
                    this.AddLogToCollection(entry);
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    lock (gate)
                        this.AddLogToCollection(entry);
                });
            }
        }

        private void AddLogToCollection(LogEntry entry)
        {
            try
            {
                Logs.Add(entry);
                while (Logs.Count > MaxLines)
                    Logs.RemoveAt(0);
            }
            catch (Exception) { }
        }
    }

    public class LogEntry
    {
        public DateTimeOffset Timestamp { get; set; }

        public LogEventLevel Level { get; set; }

        public string RoomId { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}
