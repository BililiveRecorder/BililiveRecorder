using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable
namespace BililiveRecorder.WPF.Models
{
    internal class DanmakuFileWithOffset : INotifyPropertyChanged
    {
        private string path;
        private DateTimeOffset startTime;
        private int offset;

        public string Path { get => this.path; set => this.SetField(ref this.path, value); }
        public DateTimeOffset StartTime { get => this.startTime; set => this.SetField(ref this.startTime, value); }
        public int Offset { get => this.offset; set => this.SetField(ref this.offset, value); }

        public DanmakuFileWithOffset(string path)
        {
            this.path = path;
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

        public override int GetHashCode() => HashCode.Combine(this.path, this.startTime, this.offset);
    }
}
