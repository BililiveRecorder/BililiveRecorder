namespace BililiveRecorder.FlvProcessor
{
    public delegate void TagProcessedEvent(object sender, TagProcessedArgs e);
    public class TagProcessedArgs
    {
        public IFlvTag Tag;
    }
    
    public delegate void ClipFinalizedEvent(object sender, ClipFinalizedArgs e);
    public class ClipFinalizedArgs
    {
        public IFlvClipProcessor ClipProcessor;
    }

    public delegate void StreamFinalizedEvent(object sender, StreamFinalizedArgs e);
    public class StreamFinalizedArgs
    {
        public IFlvStreamProcessor StreamProcessor;
    }

}
