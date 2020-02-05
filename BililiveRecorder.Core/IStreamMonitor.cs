using System;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    public interface IStreamMonitor : IDisposable
    {
        int Roomid { get; }
        bool IsMonitoring { get; }
        event RoomInfoUpdatedEvent RoomInfoUpdated;
        event StreamStartedEvent StreamStarted;
        event ReceivedDanmakuEvt ReceivedDanmaku;

        bool Start();
        void Stop();
        //DanmakuRecorder Setup_DanmakuRec(RecordedRoom recordedRoom);
        void Check(TriggerType type, int millisecondsDelay = 0);
        Task<RoomInfo> FetchRoomInfoAsync();
    }
}
