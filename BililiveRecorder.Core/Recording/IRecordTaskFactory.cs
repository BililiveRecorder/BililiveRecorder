namespace BililiveRecorder.Core.Recording
{
    internal interface IRecordTaskFactory
    {
        IRecordTask CreateRecordTask(IRoom room);
    }
}
