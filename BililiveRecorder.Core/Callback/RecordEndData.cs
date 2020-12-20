using System;

#nullable enable
namespace BililiveRecorder.Core.Callback
{
    public class RecordEndData
    {
        public Guid EventRandomId { get; set; } = Guid.NewGuid();

        public int RoomId { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTimeOffset StartRecordTime { get; set; }
        public DateTimeOffset EndRecordTime { get; set; }
    }
}
