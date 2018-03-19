using System;
using System.Collections.Generic;
using System.Text;

namespace BililiveRecorder.FlvProcessor
{
    public delegate void TagProcessedEvent(object sender, TagProcessedArgs e);
    public class TagProcessedArgs
    {
        public FlvTag Tag;
    }


    public delegate void ClipFinalizedEvent(object sender, ClipFinalizedArgs e);
    public class ClipFinalizedArgs
    {
        public FlvClipProcessor ClipProcessor;
    }

    public delegate void StreamFinalizedEvent(object sender, StreamFinalizedArgs e);
    public class StreamFinalizedArgs
    {
        public FlvStreamProcessor StreamProcessor;
    }

}
