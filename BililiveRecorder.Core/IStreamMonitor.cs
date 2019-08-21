using System;

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
        void Check(TriggerType type, int millisecondsDelay = 0);
        RoomInfo FetchRoomInfo();
    }
}
