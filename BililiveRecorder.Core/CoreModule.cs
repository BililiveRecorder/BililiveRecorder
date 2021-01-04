using System.Net.Sockets;
using Autofac;
using BililiveRecorder.Core.Config.V2;

#nullable enable
namespace BililiveRecorder.Core
{
    public class CoreModule : Module
    {
        public CoreModule()
        {
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(x => x.Resolve<IRecorder>().Config).As<ConfigV2>();
            builder.Register(x => x.Resolve<ConfigV2>().Global).As<GlobalConfig>();
            builder.RegisterType<BililiveAPI>().AsSelf().InstancePerMatchingLifetimeScope("recorder_root");
            builder.RegisterType<TcpClient>().AsSelf().ExternallyOwned();
            builder.RegisterType<StreamMonitor>().As<IStreamMonitor>().ExternallyOwned();
            builder.RegisterType<RecordedRoom>().As<IRecordedRoom>().ExternallyOwned();
            builder.RegisterType<BasicDanmakuWriter>().As<IBasicDanmakuWriter>().ExternallyOwned();
            builder.RegisterType<Recorder>().As<IRecorder>().InstancePerMatchingLifetimeScope("recorder_root");
        }
    }
}
