using System;

namespace BililiveRecorder.Core.Event
{
    public sealed class IOStatsEventArgs : EventArgs
    {
        /// <summary>
        /// 当前统计区间的开始时间
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// 当前统计区间的结束时间
        /// </summary>
        public DateTimeOffset EndTime { get; set; }

        /// <summary>
        /// 当前统计区间的时长
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 下载了的数据量
        /// </summary>
        public int NetworkBytesDownloaded { get; set; }

        /// <summary>
        /// 平均下载速度，mibi-bits per second
        /// </summary>
        public double NetworkMbps { get; set; }

        /// <summary>
        /// 统计区间内的磁盘写入耗时
        /// </summary>
        public TimeSpan DiskWriteDuration { get; set; }

        /// <summary>
        /// 统计区间内写入磁盘的数据量
        /// </summary>
        public int DiskBytesWritten { get; set; }

        /// <summary>
        /// 平均写入速度，mibi-bytes per second
        /// </summary>
        public double DiskMBps { get; set; }
    }
}
