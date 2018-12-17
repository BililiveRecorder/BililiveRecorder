using System;

namespace BililiveRecorder.Core
{
    public interface IStreamMonitor : IDisposable
    {
        int Roomid { get; }
        bool IsMonitoring { get; }
        event StreamStatusChangedEvent StreamStatusChanged;
        event ReceivedDanmakuEvt ReceivedDanmaku;

        bool Start();
        void Stop();
        void Check(TriggerType type, int millisecondsDelay = 0);
    }
}
