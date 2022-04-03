using System;

namespace BililiveRecorder.Web.Models
{
    public class RoomStatsDto
    {
        public TimeSpan SessionDuration { get; set; }
        public TimeSpan SessionMaxTimestamp { get; set; }
        public TimeSpan FileMaxTimestamp { get; set; }
        public double DurationRatio { get; set; }
        public long TotalInputBytes { get; set; }
        public long TotalOutputBytes { get; set; }
        public double NetworkMbps { get; set; }
    }
}
