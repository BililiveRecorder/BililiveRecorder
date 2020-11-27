using System.Net.Sockets;
using Autofac;
using BililiveRecorder.Core.Config;

namespace BililiveRecorder.Core
{
    public class CoreModule : Module
    {
        public CoreModule()
        {

        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ConfigV1>().AsSelf().InstancePerMatchingLifetimeScope("recorder_root");
            builder.RegisterType<BililiveAPI>().AsSelf().InstancePerMatchingLifetimeScope("recorder_root");
            builder.RegisterType<TcpClient>().AsSelf().ExternallyOwned();
            builder.RegisterType<StreamMonitor>().As<IStreamMonitor>().ExternallyOwned();
            builder.RegisterType<RecordedRoom>().As<IRecordedRoom>().ExternallyOwned();
            builder.RegisterType<BasicDanmakuWriter>().As<IBasicDanmakuWriter>().ExternallyOwned();
            builder.RegisterType<Recorder>().As<IRecorder>().InstancePerMatchingLifetimeScope("recorder_root");
        }
    }
}
