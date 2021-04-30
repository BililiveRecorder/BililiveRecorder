using BililiveRecorder.Core.Config.V2;

namespace BililiveRecorder.Core
{
    public interface IRoomFactory
    {
        IRoom CreateRoom(RoomConfig roomConfig, int initDelayFactor);
    }
}
