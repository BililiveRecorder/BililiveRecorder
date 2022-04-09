using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Event;

namespace BililiveRecorder.Web
{
    internal class FakeRecorderForWeb : IRecorder
    {
        private bool disposedValue;

        public ConfigV3 Config { get; } = new ConfigV3();

        public ReadOnlyObservableCollection<IRoom> Rooms { get; } = new ReadOnlyObservableCollection<IRoom>(new ObservableCollection<IRoom>());

#pragma warning disable CS0067
        public event EventHandler<AggregatedRoomEventArgs<RecordSessionStartedEventArgs>>? RecordSessionStarted;
        public event EventHandler<AggregatedRoomEventArgs<RecordSessionEndedEventArgs>>? RecordSessionEnded;
        public event EventHandler<AggregatedRoomEventArgs<RecordFileOpeningEventArgs>>? RecordFileOpening;
        public event EventHandler<AggregatedRoomEventArgs<RecordFileClosedEventArgs>>? RecordFileClosed;
        public event EventHandler<AggregatedRoomEventArgs<IOStatsEventArgs>>? IOStats;
        public event EventHandler<AggregatedRoomEventArgs<RecordingStatsEventArgs>>? RecordingStats;
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

        public IRoom AddRoom(int roomid) => null!;

        public IRoom AddRoom(int roomid, bool enabled) => null!;

        public void RemoveRoom(IRoom room)
        { }

        public void SaveConfig()
        { }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FakeRecorderForWeb()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
