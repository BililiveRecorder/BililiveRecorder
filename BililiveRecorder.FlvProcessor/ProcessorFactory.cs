using System;

namespace BililiveRecorder.FlvProcessor
{
    public class ProcessorFactory : IProcessorFactory
    {
        private readonly Func<IFlvTag> flvTagFactory;
        private readonly IFlvMetadataFactory flvMetadataFactory;

        public ProcessorFactory(Func<IFlvTag> flvTagFactory, IFlvMetadataFactory flvMetadataFactory)
        {
            this.flvTagFactory = flvTagFactory ?? throw new ArgumentNullException(nameof(flvTagFactory));
            this.flvMetadataFactory = flvMetadataFactory ?? throw new ArgumentNullException(nameof(flvMetadataFactory));
        }

        public IFlvStreamProcessor CreateStreamProcessor() => new FlvStreamProcessor(this, this.flvMetadataFactory, this.flvTagFactory);

        public IFlvClipProcessor CreateClipProcessor() => new FlvClipProcessor(this.flvTagFactory);
    }
}
