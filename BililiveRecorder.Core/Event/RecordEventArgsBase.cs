using System;

namespace BililiveRecorder.Core.Event
{
    public abstract class RecordEventArgsBase : EventArgs
    {
        public RecordEventArgsBase() { }

        public RecordEventArgsBase(IRoom room)
        {
            this.RoomId = room.RoomConfig.RoomId;
            this.ShortId = room.ShortId;
            this.Name = room.Name;
            this.Title = room.Title;
            this.AreaNameParent = room.AreaNameParent;
            this.AreaNameChild = room.AreaNameChild;
        }

        public Guid SessionId { get; set; }

        public int RoomId { get; set; }

        public int ShortId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string AreaNameParent { get; set; } = string.Empty;

        public string AreaNameChild { get; set; } = string.Empty;
    }
}
