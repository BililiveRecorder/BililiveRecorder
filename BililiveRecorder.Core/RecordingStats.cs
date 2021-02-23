using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable
namespace BililiveRecorder.Core
{
    public class RecordingStats : INotifyPropertyChanged
    {
        private TimeSpan sessionMaxTimestamp;
        private TimeSpan fileMaxTimestamp;
        private TimeSpan sessionDuration;
        private double networkMbps;
        private long totalInputBytes;
        private long totalOutputBytes;
        private double duraionRatio;

        public TimeSpan SessionDuration { get => this.sessionDuration; set => this.SetField(ref this.sessionDuration, value); }
        public TimeSpan SessionMaxTimestamp { get => this.sessionMaxTimestamp; set => this.SetField(ref this.sessionMaxTimestamp, value); }
        public TimeSpan FileMaxTimestamp { get => this.fileMaxTimestamp; set => this.SetField(ref this.fileMaxTimestamp, value); }

        public double DuraionRatio { get => this.duraionRatio; set => this.SetField(ref this.duraionRatio, value); }

        public long TotalInputBytes { get => this.totalInputBytes; set => this.SetField(ref this.totalInputBytes, value); }
        public long TotalOutputBytes { get => this.totalOutputBytes; set => this.SetField(ref this.totalOutputBytes, value); }

        public double NetworkMbps { get => this.networkMbps; set => this.SetField(ref this.networkMbps, value); }

        public void Reset()
        {
            this.SessionDuration = TimeSpan.Zero;
            this.SessionMaxTimestamp = TimeSpan.Zero;
            this.FileMaxTimestamp = TimeSpan.Zero;
            this.DuraionRatio = 0;
            this.TotalInputBytes = 0;
            this.TotalOutputBytes = 0;
            this.NetworkMbps = 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetField<T>(ref T location, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(location, value))
                return false;
            location = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
    }
}
