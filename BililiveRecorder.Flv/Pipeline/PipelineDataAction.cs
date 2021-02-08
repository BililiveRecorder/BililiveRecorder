using System;
using System.Collections.Generic;

namespace BililiveRecorder.Flv.Pipeline
{
    public class PipelineDataAction : PipelineAction
    {
        public PipelineDataAction(IList<Tag> tags)
        {
            this.Tags = tags ?? throw new ArgumentNullException(nameof(tags));
        }

        public IList<Tag> Tags { get; set; }

        public override PipelineAction Clone() => new PipelineDataAction(new List<Tag>(this.Tags));
    }
}
