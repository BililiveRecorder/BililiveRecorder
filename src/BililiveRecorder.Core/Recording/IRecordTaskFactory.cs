using BililiveRecorder.Core.Config;

namespace BililiveRecorder.Core.Recording
{
    internal interface IRecordTaskFactory
    {
        IRecordTask CreateRecordTask(IRoom room, RecordMode? recordModeOverride = null);
    }
}
