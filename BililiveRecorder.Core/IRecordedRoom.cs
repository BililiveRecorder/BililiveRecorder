using System;
using System.ComponentModel;

namespace BililiveRecorder.Core
{
    public interface IRecordedRoom : INotifyPropertyChanged
    {
        int Roomid { get; }
        int RealRoomid { get; }
        string StreamerName { get; }
        RoomInfo RoomInfo { get; }
        IRecordInfo RecordInfo { get; }

        IStreamMonitor StreamMonitor { get; }

        bool IsMonitoring { get; }
        bool IsRecording { get; }

        double DownloadSpeedKiBps { get; }
        DateTime LastUpdateDateTime { get; }

        bool Start();
        void Stop();

        void StartRecord();
        void StopRecord();

        bool UpdateRoomInfo();

        void Clip();
    }
}
