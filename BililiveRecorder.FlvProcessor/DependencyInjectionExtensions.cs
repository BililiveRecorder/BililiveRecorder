using System;
using BililiveRecorder.FlvProcessor;
using Microsoft.Extensions.DependencyInjection;

namespace BililiveRecorder.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddFlvProcessor(this IServiceCollection services) => services
            .AddSingleton<Func<IFlvTag>>(() => new FlvTag())
            .AddSingleton<IFlvMetadataFactory, FlvMetadataFactory>()
            .AddSingleton<IProcessorFactory, ProcessorFactory>()
            ;
    }
}
