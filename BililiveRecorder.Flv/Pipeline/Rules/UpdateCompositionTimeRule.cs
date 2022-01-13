using System;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    public class UpdateCompositionTimeRule : ISimpleProcessingRule
    {
        public void Run(FlvProcessingContext context, Action next)
        {
            next();
        }
    }
}
