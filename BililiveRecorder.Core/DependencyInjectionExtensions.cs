using BililiveRecorder.Core;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Api.Danmaku;
using BililiveRecorder.Core.Api.Http;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Danmaku;
using BililiveRecorder.Core.Recording;
using BililiveRecorder.Core.Scripting;
using BililiveRecorder.Core.Templating;
using BililiveRecorder.Flv;
using Microsoft.Extensions.DependencyInjection;
using Polly.Registry;

namespace BililiveRecorder.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddRecorderConfig(this IServiceCollection services, ConfigV3 config) => services
            .AddSingleton(config)
            .AddSingleton(sp => sp.GetRequiredService<ConfigV3>().Global)
            ;

        public static IServiceCollection AddRecorder(this IServiceCollection services) => services
            .AddSingleton<IMemoryStreamProvider, RecyclableMemoryStreamProvider>()
            .AddRecorderPollyPolicy()
            .AddRecorderApiClients()
            .AddRecorderRecording()
            .AddSingleton<IRecorder, Recorder>()
            .AddSingleton<IRoomFactory, RoomFactory>()
            .AddScoped<IBasicDanmakuWriter, BasicDanmakuWriter>()
            .AddSingleton<UserScriptRunner>()
            ;

        private static IServiceCollection AddRecorderPollyPolicy(this IServiceCollection services) => services
            .AddSingleton<PollyPolicy>()
            .AddSingleton<IReadOnlyPolicyRegistry<string>>(sp => sp.GetRequiredService<PollyPolicy>())
            ;

        public static IServiceCollection AddRecorderApiClients(this IServiceCollection services) => services
            .AddSingleton<HttpApiClient>()
            .AddSingleton<IHttpClientAccessor>(sp => sp.GetRequiredService<HttpApiClient>())
            .AddSingleton<PolicyWrappedApiClient<HttpApiClient>>()
            .AddSingleton<IApiClient>(sp => sp.GetRequiredService<PolicyWrappedApiClient<HttpApiClient>>())
            .AddSingleton<IDanmakuServerApiClient>(sp => sp.GetRequiredService<PolicyWrappedApiClient<HttpApiClient>>())
            .AddScoped<IDanmakuClient, DanmakuClient>()
            ;

        public static IServiceCollection AddRecorderRecording(this IServiceCollection services) => services
            .AddSingleton<FileNameGenerator>()
            .AddScoped<IRecordTaskFactory, RecordTaskFactory>()
            .AddScoped<IFlvProcessingContextWriterFactory, FlvProcessingContextWriterWithFileWriterFactory>()
            .AddScoped<IFlvTagReaderFactory, FlvTagReaderFactory>()
            .AddScoped<ITagGroupReaderFactory, TagGroupReaderFactory>()
            ;
    }
}
