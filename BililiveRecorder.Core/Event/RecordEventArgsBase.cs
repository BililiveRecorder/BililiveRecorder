using System;

namespace BililiveRecorder.Core.Event
{
    public abstract class RecordEventArgsBase : EventArgs
    {
        protected RecordEventArgsBase(IRoom room)
        {
            this.RoomId = room.RoomConfig.RoomId;
            this.ShortId = room.ShortId;
            this.Name = room.Name;
            this.Title = room.Title;
            this.AreaNameParent = room.AreaNameParent;
            this.AreaNameChild = room.AreaNameChild;
            this.Recording = room.Recording;
            this.Streaming = room.Streaming;
            this.DanmakuConnected = room.DanmakuConnected;
        }

        public int RoomId { get; protected set; }
        public int ShortId { get; protected set; }
        public string Name { get; protected set; } = string.Empty;
        public string Title { get; protected set; } = string.Empty;
        public string AreaNameParent { get; protected set; } = string.Empty;
        public string AreaNameChild { get; protected set; } = string.Empty;

        public bool Recording { get; protected set; }
        public bool Streaming { get; protected set; }
        public bool DanmakuConnected { get; protected set; }
    }
}
