namespace BililiveRecorder.Core
{
    public delegate void RoomInfoUpdatedEvent(object sender, RoomInfoUpdatedArgs e);
    public class RoomInfoUpdatedArgs
    {
        public RoomInfo RoomInfo;
    }

    public delegate void StreamStartedEvent(object sender, StreamStartedArgs e);
    public class StreamStartedArgs
    {
        public TriggerType type;
    }
}
