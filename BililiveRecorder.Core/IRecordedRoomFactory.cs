using BililiveRecorder.Core.Config.V2;

namespace BililiveRecorder.Core
{
    public interface IRecordedRoomFactory
    {
        IRecordedRoom CreateRecordedRoom(RoomConfig roomConfig);
    }
}
