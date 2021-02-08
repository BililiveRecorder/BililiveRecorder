namespace BililiveRecorder.FlvProcessor
{
    public interface IFlvMetadataFactory
    {
        IFlvMetadata CreateFlvMetadata(byte[] data);
    }
}
