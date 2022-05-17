using System;

namespace BililiveRecorder.Core.Event
{
    public interface IRecordSessionEventArgs
    {
        Guid SessionId { get; }
    }
}
