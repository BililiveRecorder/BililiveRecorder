using System;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Writer;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BililiveRecorder.Core.Recording
{
    public class FlvProcessingContextWriterWithFileWriterFactory : IFlvProcessingContextWriterFactory
    {
        private readonly IServiceProvider serviceProvider;

        public FlvProcessingContextWriterWithFileWriterFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IFlvProcessingContextWriter CreateWriter(IFlvWriterTargetProvider targetProvider) =>
            new FlvProcessingContextWriter(new FlvTagFileWriter(targetProvider,
                                                                this.serviceProvider.GetRequiredService<IMemoryStreamProvider>(),
                                                                this.serviceProvider.GetService<ILogger>()));
    }
}
