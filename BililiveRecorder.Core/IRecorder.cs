using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Event;

namespace BililiveRecorder.Core
{
    public interface IRecorder : INotifyPropertyChanged, IDisposable
    {
        ConfigV3 Config { get; }
        ReadOnlyObservableCollection<IRoom> Rooms { get; }

        event EventHandler<AggregatedRoomEventArgs<RecordSessionStartedEventArgs>>? RecordSessionStarted;
        event EventHandler<AggregatedRoomEventArgs<RecordSessionEndedEventArgs>>? RecordSessionEnded;
        event EventHandler<AggregatedRoomEventArgs<RecordFileOpeningEventArgs>>? RecordFileOpening;
        event EventHandler<AggregatedRoomEventArgs<RecordFileClosedEventArgs>>? RecordFileClosed;
        event EventHandler<AggregatedRoomEventArgs<IOStatsEventArgs>>? IOStats;
        event EventHandler<AggregatedRoomEventArgs<RecordingStatsEventArgs>>? RecordingStats;

        IRoom AddRoom(int roomid);
        IRoom AddRoom(int roomid, bool enabled);
        void RemoveRoom(IRoom room);

        void SaveConfig();
    }
}
