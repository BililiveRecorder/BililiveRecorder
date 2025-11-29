using System;
using System.Threading.Tasks;
using BililiveRecorder.Core.Event;

namespace BililiveRecorder.Core.Recording
{
    internal interface IRecordTask
    {
        Guid SessionId { get; }

        event EventHandler<IOStatsEventArgs>? IOStats;
        event EventHandler<RecordingStatsEventArgs>? RecordingStats;
        event EventHandler<RecordFileOpeningEventArgs>? RecordFileOpening;
        event EventHandler<RecordFileClosedEventArgs>? RecordFileClosed;
        event EventHandler? RecordSessionEnded;

        void SplitOutput();
        Task StartAsync();
        void RequestStop();
    }
}
