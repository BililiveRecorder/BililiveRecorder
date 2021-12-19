using System;
using System.ComponentModel;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Event;

namespace BililiveRecorder.Core
{
    public interface IRoom : INotifyPropertyChanged, IDisposable
    {
        Guid ObjectId { get; }

        RoomConfig RoomConfig { get; }

        int ShortId { get; }
        string Name { get; }
        string Title { get; }
        string AreaNameParent { get; }
        string AreaNameChild { get; }

        bool Recording { get; }
        bool Streaming { get; }
        bool DanmakuConnected { get; }
        bool AutoRecordForThisSession { get; }
        RecordingStats Stats { get; }

        event EventHandler<RecordSessionStartedEventArgs>? RecordSessionStarted;
        event EventHandler<RecordSessionEndedEventArgs>? RecordSessionEnded;
        event EventHandler<RecordFileOpeningEventArgs>? RecordFileOpening;
        event EventHandler<RecordFileClosedEventArgs>? RecordFileClosed;
        event EventHandler<RecordingStatsEventArgs>? RecordingStats;
        event EventHandler<NetworkingStatsEventArgs>? NetworkingStats;

        void StartRecord();
        void StopRecord();
        void SplitOutput();
        Task RefreshRoomInfoAsync();
    }
}
