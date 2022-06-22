using System;
using System.Linq;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Pipeline.Actions;

namespace BililiveRecorder.ToolBox.ProcessingRules
{
    public class FfmpegDetectionRule : ISimpleProcessingRule
    {
        public bool EndTagDetected { get; private set; }
        public bool LavfEncoderDetected { get; private set; }

        public void Run(FlvProcessingContext context, Action next)
        {
            if (!this.EndTagDetected && context.Actions.Any(x => x is PipelineEndAction))
            {
                this.EndTagDetected = true;
            }

            if (!this.LavfEncoderDetected)
            {
                if (context.Actions.Any(action =>
                {
                    if (action is PipelineScriptAction scriptAction)
                    {
                        var encoder = scriptAction?.Tag?.ScriptData?.GetMetadataValue()?.Value?["encoder"] as ScriptDataString;
                        if (encoder is not null)
                        {
                            return encoder.Value.StartsWith("Lavf", StringComparison.Ordinal);
                        }
                    }
                    return false;
                }))
                {
                    this.LavfEncoderDetected = true;
                }
            }
        }
    }
}
