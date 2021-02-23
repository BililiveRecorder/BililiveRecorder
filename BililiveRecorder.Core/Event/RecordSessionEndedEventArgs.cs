namespace BililiveRecorder.Core.Event
{
    public class RecordSessionEndedEventArgs : RecordEventArgsBase
    {
        public RecordSessionEndedEventArgs() { }

        public RecordSessionEndedEventArgs(IRoom room) : base(room) { }
    }
}
