using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable
namespace BililiveRecorder.Desktop.Models
{
    /// <summary>弹幕合并列表中的一项：XML 文件与其相对时间偏移（秒）。</summary>
    public class DanmakuFileWithOffset : INotifyPropertyChanged
    {
        private string path = string.Empty;
        private DateTimeOffset startTime;
        private int offset;

        public string Path
        {
            get => this.path;
            set => this.SetField(ref this.path, value);
        }

        public DateTimeOffset StartTime
        {
            get => this.startTime;
            set => this.SetField(ref this.startTime, value);
        }

        public int Offset
        {
            get => this.offset;
            set => this.SetField(ref this.offset, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
