using System;

namespace BililiveRecorder.Flv.Pipeline
{
    public interface IProcessingPipelineBuilder
    {
        IServiceProvider ServiceProvider { get; }

        IProcessingPipelineBuilder Add(Func<ProcessingDelegate, ProcessingDelegate> rule);

        ProcessingDelegate Build();
    }
}
