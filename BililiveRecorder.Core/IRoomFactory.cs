using BililiveRecorder.Core.Config.V3;

namespace BililiveRecorder.Core
{
    public interface IRoomFactory
    {
        IRoom CreateRoom(RoomConfig roomConfig, int initDelayFactor);
    }
}
