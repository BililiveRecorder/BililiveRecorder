using System;
using BililiveRecorder.FlvProcessor;
using Microsoft.Extensions.DependencyInjection;

namespace BililiveRecorder.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static void AddFlvProcessor(this IServiceCollection services)
        {
            services.AddSingleton<Func<IFlvTag>>(() => new FlvTag());
            services.AddSingleton<IFlvMetadataFactory, FlvMetadataFactory>();
            services.AddSingleton<IProcessorFactory, ProcessorFactory>();
        }
    }
}
