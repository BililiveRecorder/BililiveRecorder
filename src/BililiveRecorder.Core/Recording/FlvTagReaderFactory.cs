using System;
using System.IO.Pipelines;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Parser;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BililiveRecorder.Core.Recording
{
    internal class FlvTagReaderFactory : IFlvTagReaderFactory
    {
        private readonly IServiceProvider serviceProvider;

        public FlvTagReaderFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IFlvTagReader CreateFlvTagReader(PipeReader pipeReader) =>
            new FlvTagPipeReader(pipeReader, this.serviceProvider.GetRequiredService<IMemoryStreamProvider>(), this.serviceProvider.GetService<ILogger>());
    }
}
