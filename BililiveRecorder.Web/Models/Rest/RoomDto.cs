using System;

namespace BililiveRecorder.Web.Models.Rest
{
    public class RoomDto
    {
        public Guid ObjectId { get; set; }
        public int RoomId { get; set; }
        public bool AutoRecord { get; set; }
        public int ShortId { get; set; }
        public string? Name { get; set; }
        public string? Title { get; set; }
        public string? AreaNameParent { get; set; }
        public string? AreaNameChild { get; set; }
        public bool Recording { get; set; }
        public bool Streaming { get; set; }
        public bool DanmakuConnected { get; set; }
        public bool AutoRecordForThisSession { get; set; }
        public RoomRecordingStatsDto RecordingStats { get; set; } = default!;
        public RoomIOStatsDto IoStats { get; set; } = default!;
    }
}
