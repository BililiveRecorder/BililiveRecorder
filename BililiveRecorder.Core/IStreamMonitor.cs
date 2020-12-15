using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    public interface IStreamMonitor : IDisposable, INotifyPropertyChanged
    {
        int Roomid { get; }
        bool IsMonitoring { get; }
        bool IsDanmakuConnected { get; }
        event RoomInfoUpdatedEvent RoomInfoUpdated;
        event StreamStartedEvent StreamStarted;
        event ReceivedDanmakuEvt ReceivedDanmaku;

        bool Start();
        void Stop();
        void Check(TriggerType type, int millisecondsDelay = 0);
        Task<RoomInfo> FetchRoomInfoAsync();
    }
}
