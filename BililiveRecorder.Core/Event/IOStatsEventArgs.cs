using System;

namespace BililiveRecorder.Core.Event
{
    public class IOStatsEventArgs : EventArgs
    {
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public TimeSpan Duration { get; set; }

        public int NetworkBytesDownloaded { get; set; }

        public double NetworkMbps { get; set; }
    }
}
