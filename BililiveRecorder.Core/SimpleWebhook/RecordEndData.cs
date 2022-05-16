using System;
using BililiveRecorder.Core.Event;

#nullable enable
namespace BililiveRecorder.Core.SimpleWebhook
{
    internal class RecordEndData
    {
        public RecordEndData(RecordFileClosedEventArgs args)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            this.RoomId = args.RoomId;
            this.Name = args.Name;
            this.Title = args.Title;
            this.RelativePath = args.RelativePath;
            this.FileSize = args.FileSize;
            this.StartRecordTime = args.FileOpenTime;
            this.EndRecordTime = args.FileCloseTime;
        }

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
