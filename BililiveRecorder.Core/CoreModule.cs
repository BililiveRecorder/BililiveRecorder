using Autofac;
using System.Net.Sockets;

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
            builder.RegisterType<TcpClient>().AsSelf().ExternallyOwned();
            builder.RegisterType<DanmakuReceiver>().As<IDanmakuReceiver>();
            builder.RegisterType<StreamMonitor>().As<IStreamMonitor>();
            builder.RegisterType<RecordInfo>().As<IRecordInfo>();
            builder.RegisterType<RecordedRoom>().As<IRecordedRoom>();
            builder.RegisterType<Recorder>().AsSelf().InstancePerMatchingLifetimeScope("recorder_root");
        }
    }
}
