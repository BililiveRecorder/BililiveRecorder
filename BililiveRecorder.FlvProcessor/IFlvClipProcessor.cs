using System.Collections.Generic;

namespace BililiveRecorder.FlvProcessor
{
    public interface IFlvClipProcessor
    {
        IFlvMetadata Header { get; }
        List<IFlvTag> HTags { get; }
        List<IFlvTag> Tags { get; }

        IFlvClipProcessor Initialize(string path, IFlvMetadata metadata, List<IFlvTag> head, List<IFlvTag> data, uint seconds);
        void AddTag(IFlvTag tag);
        void FinallizeFile();
        event ClipFinalizedEvent ClipFinalized;

    }
}
