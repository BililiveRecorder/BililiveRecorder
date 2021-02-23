using System;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Writer;
using Microsoft.Extensions.DependencyInjection;

namespace BililiveRecorder.Core.Recording
{
    public class FlvProcessingContextWriterFactory : IFlvProcessingContextWriterFactory
    {
        private readonly IServiceProvider serviceProvider;

        public FlvProcessingContextWriterFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IFlvProcessingContextWriter CreateWriter(IFlvWriterTargetProvider targetProvider) =>
            new FlvProcessingContextWriter(targetProvider, this.serviceProvider.GetRequiredService<IMemoryStreamProvider>(), null);
    }
}
