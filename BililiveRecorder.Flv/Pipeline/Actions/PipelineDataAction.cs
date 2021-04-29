using System;
using System.Collections.Generic;

namespace BililiveRecorder.Flv.Pipeline.Actions
{
    public class PipelineDataAction : PipelineAction
    {
        public PipelineDataAction(IReadOnlyList<Tag> tags)
        {
            this.Tags = tags ?? throw new ArgumentNullException(nameof(tags));
        }

        public IReadOnlyList<Tag> Tags { get; set; }

        public override PipelineAction Clone() => new PipelineDataAction(new List<Tag>(this.Tags));
    }
}
