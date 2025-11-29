using System;
using BililiveRecorder.Core.SimpleWebhook;

namespace BililiveRecorder.Core.Event
{
    /// <summary>
    /// <see cref="EventType.SessionEnded"/>
    /// </summary>
    public sealed class RecordSessionEndedEventArgs : RecordEventArgsBase, IRecordSessionEventArgs
    {
        internal RecordSessionEndedEventArgs(IRoom room) : base(room) { }

        public Guid SessionId { get; set; }
    }
}
