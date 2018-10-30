using System.Collections.Generic;

namespace BililiveRecorder.FlvProcessor
{
    public interface IFlvMetadata
    {
        IDictionary<string, object> Meta { get; set; }
        byte[] ToBytes();
    }
}
