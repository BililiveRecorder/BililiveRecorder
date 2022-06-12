using System;
using Serilog.Events;

namespace BililiveRecorder.Flv.Pipeline.Actions
{
    public class PipelineLogMessageWithLocationAction : PipelineAction
    {
        public PipelineLogMessageWithLocationAction(LogEventLevel level, string message)
        {
            this.Level = level;
            this.Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public LogEventLevel Level { get; }

        public string Message { get; }

        public override PipelineAction Clone() => new PipelineLogMessageWithLocationAction(this.Level, this.Message);
    }
}
