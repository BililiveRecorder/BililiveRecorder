using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace BililiveRecorder.WPF.Models
{
    internal class LogModel : ObservableCollection<string>, IDisposable
    {
        private const int MAX_LINE = 50;

        private bool disposedValue;

        public static void AddLog(string log) => LogReceived?.Invoke(null, log);
        public static event EventHandler<string> LogReceived;

        public LogModel() : base(new[] { "" })
        {
            LogReceived += LogModel_LogReceived;
        }

        private void LogModel_LogReceived(object sender, string e)
        {
            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, (Action<string>)AddLogToCollection, e);
        }

        private void AddLogToCollection(string e)
        {
            Add(e);
            while (Count > MAX_LINE)
            {
                RemoveItem(0);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    LogReceived -= LogModel_LogReceived;
                    ClearItems();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LogModel()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
