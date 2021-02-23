namespace BililiveRecorder.Core.Event
{
    public class RecordSessionStartedEventArgs : RecordEventArgsBase
    {
        public RecordSessionStartedEventArgs() { }

        public RecordSessionStartedEventArgs(IRoom room) : base(room) { }
    }
}
