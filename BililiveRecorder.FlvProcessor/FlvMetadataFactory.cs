namespace BililiveRecorder.FlvProcessor
{
    public class FlvMetadataFactory : IFlvMetadataFactory
    {
        public IFlvMetadata CreateFlvMetadata(byte[] data) => new FlvMetadata(data);
    }
}
