using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BililiveRecorder.Core;

namespace BililiveRecorder.WPF.Models
{
    internal class RootModel : INotifyPropertyChanged, IDisposable
    {
        private bool disposedValue;
        private IRecorder recorder;

        public event PropertyChangedEventHandler PropertyChanged;

        public LogModel Logs { get; } = new LogModel();

        public IRecorder Recorder { get => this.recorder; internal set => this.SetField(ref this.recorder, value); }

        public RootModel()
        {
        }

        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) { return false; }
            field = value; this.OnPropertyChanged(propertyName); return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.Recorder?.Dispose();
                    this.Logs.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                this.disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~RootModel()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
