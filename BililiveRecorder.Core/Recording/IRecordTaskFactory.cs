namespace BililiveRecorder.Core.Recording
{
    public interface IRecordTaskFactory
    {
        IRecordTask CreateRecordTask(IRoom room);
    }
}
