using Autofac;

namespace BililiveRecorder.Core
{
    public class CoreModule : Module
    {
        public CoreModule()
        {

        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<Settings>().As<ISettings>().InstancePerMatchingLifetimeScope("recorder_root");
            builder.RegisterType<StreamMonitor>().As<IStreamMonitor>();
            builder.RegisterType<RecordInfo>().As<IRecordInfo>();
            builder.RegisterType<RecordedRoom>().As<IRecordedRoom>();
            builder.RegisterType<Recorder>().AsSelf();
        }
    }
}
