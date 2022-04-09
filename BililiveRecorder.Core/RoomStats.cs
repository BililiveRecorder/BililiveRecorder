using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable
namespace BililiveRecorder.Core
{
    public class RoomStats : INotifyPropertyChanged
    {
        #region IO Stats Fields
        private DateTimeOffset ___StartTime;
        private DateTimeOffset ___EndTime;
        private TimeSpan ___Duration;
        private int ___NetworkBytesDownloaded;
        private double ___NetworkMbps;
        private TimeSpan ___DiskWriteDuration;
        private int ___DiskBytesWritten;
        private double ___DiskMBps;
        #endregion

        #region Recording Stats Fields
        private TimeSpan ___SessionDuration;
        private long ___TotalInputBytes;
        private long ___TotalOutputBytes;
        private long ___CurrentFileSize;
        private TimeSpan ___SessionMaxTimestamp;
        private TimeSpan ___FileMaxTimestamp;
        private double ___AddedDuration;
        private double ___PassedTime;
        private double ___DurationRatio;

        private long ___InputVideoBytes;
        private long ___InputAudioBytes;

        private int ___OutputVideoFrames;
        private int ___OutputAudioFrames;
        private long ___OutputVideoBytes;
        private long ___OutputAudioBytes;

        private long ___TotalInputVideoBytes;
        private long ___TotalInputAudioBytes;

        private int ___TotalOutputVideoFrames;
        private int ___TotalOutputAudioFrames;
        private long ___TotalOutputVideoBytes;
        private long ___TotalOutputAudioBytes;
        #endregion

        public RoomStats()
        {
            this.Reset();
        }

        #region IO Stats Properties

        /// <summary>
        /// 当前统计区间的开始时间
        /// </summary>
        public DateTimeOffset StartTime { get => this.___StartTime; set => this.SetField(ref this.___StartTime, value); }

        /// <summary>
        /// 当前统计区间的结束时间
        /// </summary>
        public DateTimeOffset EndTime { get => this.___EndTime; set => this.SetField(ref this.___EndTime, value); }

        /// <summary>
        /// 当前统计区间的时长
        /// </summary>
        public TimeSpan Duration { get => this.___Duration; set => this.SetField(ref this.___Duration, value); }

        /// <summary>
        /// 下载了的数据量
        /// </summary>
        public int NetworkBytesDownloaded { get => this.___NetworkBytesDownloaded; set => this.SetField(ref this.___NetworkBytesDownloaded, value); }

        /// <summary>
        /// 平均下载速度，mibi-bits per second
        /// </summary>
        public double NetworkMbps { get => this.___NetworkMbps; set => this.SetField(ref this.___NetworkMbps, value); }

        /// <summary>
        /// 统计区间内的磁盘写入耗时
        /// </summary>
        public TimeSpan DiskWriteDuration { get => this.___DiskWriteDuration; set => this.SetField(ref this.___DiskWriteDuration, value); }

        /// <summary>
        /// 统计区间内写入磁盘的数据量
        /// </summary>
        public int DiskBytesWritten { get => this.___DiskBytesWritten; set => this.SetField(ref this.___DiskBytesWritten, value); }

        /// <summary>
        /// 平均写入速度，mibi-bytes per second
        /// </summary>
        public double DiskMBps { get => this.___DiskMBps; set => this.SetField(ref this.___DiskMBps, value); }

        #endregion
        #region Recording Stats Properties

        /// <summary>
        /// 从录制开始到现在一共经过的时间
        /// </summary>
        public TimeSpan SessionDuration { get => this.___SessionDuration; set => this.SetField(ref this.___SessionDuration, value); }

        /// <summary>
        /// 总接受字节数
        /// </summary>
        public long TotalInputBytes { get => this.___TotalInputBytes; set => this.SetField(ref this.___TotalInputBytes, value); }

        /// <summary>
        /// 总写入字节数
        /// </summary>
        public long TotalOutputBytes { get => this.___TotalOutputBytes; set => this.SetField(ref this.___TotalOutputBytes, value); }

        /// <summary>
        /// 当前文件的大小
        /// </summary>
        public long CurrentFileSize { get => this.___CurrentFileSize; set => this.SetField(ref this.___CurrentFileSize, value); }

        /// <summary>
        /// 本次直播流收到的最大时间戳（已修复过，相当于总时长）
        /// </summary>
        public TimeSpan SessionMaxTimestamp { get => this.___SessionMaxTimestamp; set => this.SetField(ref this.___SessionMaxTimestamp, value); }

        /// <summary>
        /// 当前文件的最大时间戳（相当于总时长）
        /// </summary>
        public TimeSpan FileMaxTimestamp { get => this.___FileMaxTimestamp; set => this.SetField(ref this.___FileMaxTimestamp, value); }

        /// <summary>
        /// 当前这一个统计区间的直播数据时长
        /// </summary>
        public double AddedDuration { get => this.___AddedDuration; set => this.SetField(ref this.___AddedDuration, value); }

        /// <summary>
        /// 当前这一个统计区间所经过的时间长度
        /// </summary>
        public double PassedTime { get => this.___PassedTime; set => this.SetField(ref this.___PassedTime, value); }

        /// <summary>
        /// 录制速度比例
        /// </summary>
        public double DurationRatio { get => this.___DurationRatio; set => this.SetField(ref this.___DurationRatio, value); }

        //----------------------------------------

        /// <summary>
        /// 当前统计区间新收到的视频数据大小
        /// </summary>
        public long InputVideoBytes { get => this.___InputVideoBytes; set => this.SetField(ref this.___InputVideoBytes, value); }
        /// <summary>
        /// 当前统计区间新收到的音频数据大小
        /// </summary>
        public long InputAudioBytes { get => this.___InputAudioBytes; set => this.SetField(ref this.___InputAudioBytes, value); }

        /// <summary>
        /// 当前统计区间新写入的视频帧数量
        /// </summary>
        public int OutputVideoFrames { get => this.___OutputVideoFrames; set => this.SetField(ref this.___OutputVideoFrames, value); }
        /// <summary>
        /// 当前统计区间新写入的音频帧数量
        /// </summary>
        public int OutputAudioFrames { get => this.___OutputAudioFrames; set => this.SetField(ref this.___OutputAudioFrames, value); }
        /// <summary>
        /// 当前统计区间新写入的视频数据大小
        /// </summary>
        public long OutputVideoBytes { get => this.___OutputVideoBytes; set => this.SetField(ref this.___OutputVideoBytes, value); }
        /// <summary>
        /// 当前统计区间新写入的音频数据大小
        /// </summary>
        public long OutputAudioBytes { get => this.___OutputAudioBytes; set => this.SetField(ref this.___OutputAudioBytes, value); }

        /// <summary>
        /// 总共收到的视频数据大小
        /// </summary>
        public long TotalInputVideoBytes { get => this.___TotalInputVideoBytes; set => this.SetField(ref this.___TotalInputVideoBytes, value); }
        /// <summary>
        /// 总共收到的音频数据大小
        /// </summary>
        public long TotalInputAudioBytes { get => this.___TotalInputAudioBytes; set => this.SetField(ref this.___TotalInputAudioBytes, value); }

        /// <summary>
        /// 总共写入的视频帧数量
        /// </summary>
        public int TotalOutputVideoFrames { get => this.___TotalOutputVideoFrames; set => this.SetField(ref this.___TotalOutputVideoFrames, value); }
        /// <summary>
        /// 总共写入的音频帧数量
        /// </summary>
        public int TotalOutputAudioFrames { get => this.___TotalOutputAudioFrames; set => this.SetField(ref this.___TotalOutputAudioFrames, value); }
        /// <summary>
        /// 总共写入的视频数据大小
        /// </summary>
        public long TotalOutputVideoBytes { get => this.___TotalOutputVideoBytes; set => this.SetField(ref this.___TotalOutputVideoBytes, value); }
        /// <summary>
        /// 总共写入的音频数据大小
        /// </summary>
        public long TotalOutputAudioBytes { get => this.___TotalOutputAudioBytes; set => this.SetField(ref this.___TotalOutputAudioBytes, value); }

        #endregion

        public void Reset()
        {
            this.SessionDuration = TimeSpan.Zero;
            this.SessionMaxTimestamp = TimeSpan.Zero;
            this.FileMaxTimestamp = TimeSpan.Zero;
            //this.DurationRatio = double.NaN;

            // ------------------------------

            this.StartTime = default;
            this.EndTime = default;
            this.Duration = default;
            this.NetworkBytesDownloaded = default;
            this.NetworkMbps = default;
            this.DiskWriteDuration = default;
            this.DiskBytesWritten = default;
            this.DiskMBps = default;

            // ------------------------------

            this.SessionDuration = default;
            this.TotalInputBytes = default;
            this.TotalOutputBytes = default;
            this.CurrentFileSize = default;
            this.SessionMaxTimestamp = default;
            this.FileMaxTimestamp = default;
            this.AddedDuration = default;
            this.PassedTime = default;
            this.DurationRatio = double.NaN;

            this.InputVideoBytes = default;
            this.InputAudioBytes = default;

            this.OutputVideoFrames = default;
            this.OutputAudioFrames = default;
            this.OutputVideoBytes = default;
            this.OutputAudioBytes = default;

            this.TotalInputVideoBytes = default;
            this.TotalInputAudioBytes = default;

            this.TotalOutputVideoFrames = default;
            this.TotalOutputAudioFrames = default;
            this.TotalOutputVideoBytes = default;
            this.TotalOutputAudioBytes = default;

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
