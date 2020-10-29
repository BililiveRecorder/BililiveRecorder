using BililiveRecorder.FlvProcessor;
using System;
using System.Dynamic;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    public interface IStreamMonitor : IDisposable
    {
        int Roomid { get; }
        string StreamerName { get; set; }
        DateTime Time { get; set; }
        string path{ get; set; }
        string Title { get; set; }
        bool IsRecording { get; set; }
        bool IsMonitoring { get; }
        event RoomInfoUpdatedEvent RoomInfoUpdated;
        event StreamStartedEvent StreamStarted;
        event ReceivedDanmakuEvt ReceivedDanmaku;
        bool Start();
        void Stop();
        void Check(TriggerType type, int millisecondsDelay = 0);
        Task<RoomInfo> FetchRoomInfoAsync();
        Task<bool> ConnectAsync();
    }
}
