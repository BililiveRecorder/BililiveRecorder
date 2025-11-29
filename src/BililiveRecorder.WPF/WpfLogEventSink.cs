using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using BililiveRecorder.Core;
using Serilog.Core;
using Serilog.Events;

#nullable enable
namespace BililiveRecorder.WPF
{
    internal class WpfLogEventSink : ILogEventSink
    {
        private const int MAX_LINE = 150;
        internal static object _lock = new object();
        internal static ObservableCollection<LogModel> Logs = new ObservableCollection<LogModel>();

        public WpfLogEventSink() { }

        public void Emit(LogEvent logEvent)
        {
            var msg = logEvent.RenderMessage();
            if (logEvent.Exception != null)
                msg += " " + logEvent.Exception.Message;

            var m = new LogModel
            {
                Timestamp = logEvent.Timestamp,
                Level = logEvent.Level,
                Message = msg,
            };

            if (logEvent.Properties.TryGetValue(LoggingContext.RoomId, out var propertyValue)
                && propertyValue is ScalarValue scalarValue
                && scalarValue.Value is int roomid)
            {
                m.RoomId = roomid.ToString();
            }

            var current = Application.Current;
            if (current is null)
                lock (_lock)
                    this.AddLogToCollection(m);
            else
                _ = current.Dispatcher.BeginInvoke((Action<LogModel>)this.AddLogToCollection, m);
        }

        private void AddLogToCollection(LogModel model)
        {
            try
            {
                Logs.Add(model);
                while (Logs.Count > MAX_LINE)
                    Logs.RemoveAt(0);
            }
            catch (Exception) { }
        }

        public class LogModel : INotifyPropertyChanged
        {
            public DateTimeOffset Timestamp { get; set; }

            public LogEventLevel Level { get; set; }

            public string RoomId { get; set; } = string.Empty;

            public string Message { get; set; } = string.Empty;
#pragma warning disable CS0067
            public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
        }
    }
}
