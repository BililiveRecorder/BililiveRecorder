using System;

namespace BililiveRecorder.Flv.Pipeline.Actions
{
    public class PipelineLogMessageWithLocationAction : PipelineAction
    {
        public PipelineLogMessageWithLocationAction(string message)
        {
            this.Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public string Message { get; }

        public override PipelineAction Clone() => new PipelineLogMessageWithLocationAction(this.Message);
    }
}
