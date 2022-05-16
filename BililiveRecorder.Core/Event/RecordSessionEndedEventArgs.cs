namespace BililiveRecorder.Core.Event
{
    public sealed class RecordSessionEndedEventArgs : RecordEventArgsBase
    {
        public RecordSessionEndedEventArgs() { }

        public RecordSessionEndedEventArgs(IRoom room) : base(room) { }
    }
}
