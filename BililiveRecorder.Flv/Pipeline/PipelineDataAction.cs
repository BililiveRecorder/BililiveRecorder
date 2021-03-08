using System;
using System.Collections.Generic;

namespace BililiveRecorder.Flv.Pipeline
{
    public class PipelineDataAction : PipelineAction
    {
        public PipelineDataAction(List<Tag> tags)
        {
            this.Tags = tags ?? throw new ArgumentNullException(nameof(tags));
        }

        public List<Tag> Tags { get; set; }

        public override PipelineAction Clone() => new PipelineDataAction(new List<Tag>(this.Tags));
    }
}
