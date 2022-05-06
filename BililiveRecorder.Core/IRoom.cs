using System;
using System.ComponentModel;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Event;
using Newtonsoft.Json.Linq;

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

        JObject? RawBilibiliApiJsonData { get; }

        bool Recording { get; }
        bool Streaming { get; }
        bool DanmakuConnected { get; }
        bool AutoRecordForThisSession { get; }
        RoomStats Stats { get; }

        event EventHandler<RecordSessionStartedEventArgs>? RecordSessionStarted;
        event EventHandler<RecordSessionEndedEventArgs>? RecordSessionEnded;
        event EventHandler<RecordFileOpeningEventArgs>? RecordFileOpening;
        event EventHandler<RecordFileClosedEventArgs>? RecordFileClosed;
        event EventHandler<RecordingStatsEventArgs>? RecordingStats;
        event EventHandler<IOStatsEventArgs>? IOStats;

        void StartRecord();
        void StopRecord();
        void SplitOutput();
        Task RefreshRoomInfoAsync();
    }
}
