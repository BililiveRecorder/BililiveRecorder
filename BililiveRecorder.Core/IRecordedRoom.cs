using System;
using System.ComponentModel;
using BililiveRecorder.Core.Callback;
using BililiveRecorder.Core.Config.V2;
using BililiveRecorder.FlvProcessor;

#nullable enable
namespace BililiveRecorder.Core
{
    public interface IRecordedRoom : INotifyPropertyChanged, IDisposable
    {
        Guid Guid { get; }

        RoomConfig RoomConfig { get; }

        int ShortRoomId { get; }
        int RoomId { get; }
        string StreamerName { get; }
        string Title { get; }

        event EventHandler<RecordEndData>? RecordEnded;

        IStreamMonitor StreamMonitor { get; }
        IFlvStreamProcessor? Processor { get; }

        bool IsMonitoring { get; }
        bool IsRecording { get; }
        bool IsDanmakuConnected { get; }
        bool IsStreaming { get; }

        double DownloadSpeedPersentage { get; }
        double DownloadSpeedMegaBitps { get; }
        DateTime LastUpdateDateTime { get; }

        void Clip();

        bool Start();
        void Stop();

        void StartRecord();
        void StopRecord();

        void RefreshRoomInfo();

        void Shutdown();
    }
}
