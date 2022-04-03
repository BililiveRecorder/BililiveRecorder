namespace BililiveRecorder.Web.Models.Rest
{
    public class RoomStatsDto
    {
        public double SessionDuration { get; set; }
        public double SessionMaxTimestamp { get; set; }
        public double FileMaxTimestamp { get; set; }
        public double DurationRatio { get; set; }
        public long TotalInputBytes { get; set; }
        public long TotalOutputBytes { get; set; }
        public double NetworkMbps { get; set; }
    }
}
