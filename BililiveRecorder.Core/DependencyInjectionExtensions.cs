using BililiveRecorder.Core;
using BililiveRecorder.Core.Config.V2;
using Microsoft.Extensions.DependencyInjection;

namespace BililiveRecorder.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static void AddCore(this IServiceCollection services)
        {
            services.AddSingleton<IRecorder, Recorder>();
#pragma warning disable IDE0001
            services.AddSingleton<ConfigV2>(x => x.GetRequiredService<IRecorder>().Config);
            services.AddSingleton<GlobalConfig>(x => x.GetRequiredService<ConfigV2>().Global);
#pragma warning restore IDE0001
            services.AddSingleton<BililiveAPI>();
            services.AddSingleton<IRecordedRoomFactory, RecordedRoomFactory>();
        }
    }
}
