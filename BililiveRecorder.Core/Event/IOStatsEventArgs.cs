using System;

namespace BililiveRecorder.Core.Event
{
    public class IOStatsEventArgs : EventArgs
    {
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public TimeSpan Duration { get; set; }

        public int NetworkBytesDownloaded { get; set; }

        /// <summary>
        /// mibi-bits per seconds
        /// </summary>
        public double NetworkMbps { get; set; }

        public TimeSpan DiskWriteTime { get; set; }

        public int DiskBytesWritten { get; set; }

        /// <summary>
        /// mibi-bytes per seconds
        /// </summary>
        public double DiskMBps { get; set; }
    }
}
