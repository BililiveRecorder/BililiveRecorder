using BililiveRecorder.Flv.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace BililiveRecorder.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddFlv(this IServiceCollection services) => services
            .AddTransient<IProcessingPipelineBuilder, ProcessingPipelineBuilder>()
            ;
    }
}
