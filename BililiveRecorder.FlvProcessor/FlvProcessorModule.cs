using Autofac;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvProcessorModule : Module
    {
        public FlvProcessorModule()
        {

        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new FlvTag()).As<IFlvTag>();
            builder.RegisterType<FlvMetadata>().As<IFlvMetadata>();
            builder.RegisterType<FlvClipProcessor>().As<IFlvClipProcessor>();
            builder.RegisterType<FlvStreamProcessor>().As<IFlvStreamProcessor>();
        }
    }
}
