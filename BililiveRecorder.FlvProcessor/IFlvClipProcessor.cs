using System;
using System.Collections.Generic;

namespace BililiveRecorder.FlvProcessor
{
    public interface IFlvClipProcessor
    {
        IFlvMetadata Header { get; }
        List<IFlvTag> HTags { get; }
        List<IFlvTag> Tags { get; }
        Func<string> GetFileName { get; set; }

        void AddTag(IFlvTag tag);
        void FinallizeFile();
        event ClipFinalizedEvent ClipFinalized;

    }
}
