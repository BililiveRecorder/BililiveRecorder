namespace BililiveRecorder.Core.Event
{
    public sealed class RecordSessionStartedEventArgs : RecordEventArgsBase
    {
        public RecordSessionStartedEventArgs() { }

        public RecordSessionStartedEventArgs(IRoom room) : base(room) { }
    }
}
