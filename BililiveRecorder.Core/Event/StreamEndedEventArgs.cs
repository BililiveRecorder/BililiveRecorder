using BililiveRecorder.Core.SimpleWebhook;

namespace BililiveRecorder.Core.Event
{
    /// <summary>
    /// <see cref="EventType.StreamEnded"/>
    /// </summary>
    public sealed class StreamEndedEventArgs : RecordEventArgsBase
    {
        internal StreamEndedEventArgs(IRoom room) : base(room)
        {
        }
    }
}
