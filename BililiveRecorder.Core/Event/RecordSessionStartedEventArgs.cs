using System;
using BililiveRecorder.Core.SimpleWebhook;

namespace BililiveRecorder.Core.Event
{
    /// <summary>
    /// <see cref="EventType.SessionStarted"/>
    /// </summary>
    public sealed class RecordSessionStartedEventArgs : RecordEventArgsBase, IRecordSessionEventArgs
    {
        internal RecordSessionStartedEventArgs(IRoom room) : base(room) { }

        public Guid SessionId { get; set; }
    }
}
