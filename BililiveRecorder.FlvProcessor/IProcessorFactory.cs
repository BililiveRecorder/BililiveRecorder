namespace BililiveRecorder.FlvProcessor
{
    public interface IProcessorFactory
    {
        IFlvClipProcessor CreateClipProcessor();
        IFlvStreamProcessor CreateStreamProcessor();
    }
}
