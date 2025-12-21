using System;

namespace BililiveRecorder.Flv.Pipeline.Actions
{
    public class PipelineScriptAction : PipelineAction
    {
        public PipelineScriptAction(Tag tag)
        {
            this.Tag = tag ?? throw new ArgumentNullException(nameof(tag));
        }

        public Tag Tag { get; set; }

        public override PipelineAction Clone() => new PipelineScriptAction(this.Tag);
    }
}
