using BililiveRecorder.FlvProcessor;
using System;
using System.ComponentModel;

namespace BililiveRecorder.Core
{
    public interface IRecordedRoom : INotifyPropertyChanged, IDisposable
    {
        int ShortRoomId { get; }
        int RoomId { get; }
        string StreamerName { get; }

        IStreamMonitor StreamMonitor { get; }
        IFlvStreamProcessor Processor { get; }

        bool IsMonitoring { get; }
        bool IsRecording { get; }

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
