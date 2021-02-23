using System;

namespace BililiveRecorder.Core.Event
{
    public class NetworkingStatsEventArgs : EventArgs
    {
        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public TimeSpan Duration { get; set; }

        public int BytesDownloaded { get; set; }

        public double Mbps { get; set; }
    }
}
