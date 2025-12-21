using System;

namespace BililiveRecorder.Flv.Pipeline
{
    public interface ISimpleProcessingRule : IProcessingRule
    {
        void Run(FlvProcessingContext context, Action next);
    }
}
