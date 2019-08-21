using BililiveRecorder.FlvProcessor;
using System;
using System.ComponentModel;

namespace BililiveRecorder.Core
{
    public interface IRecordedRoom : INotifyPropertyChanged, IDisposable
    {
        int Roomid { get; }
        int RealRoomid { get; }
        string StreamerName { get; }

        IStreamMonitor StreamMonitor { get; }
        IFlvStreamProcessor Processor { get; }

        bool IsMonitoring { get; }
        bool IsRecording { get; }

        double DownloadSpeedPersentage { get; }
        double DownloadSpeedKiBps { get; }
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
