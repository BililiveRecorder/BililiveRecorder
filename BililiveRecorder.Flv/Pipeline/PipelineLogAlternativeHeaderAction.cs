using System;
using System.Collections.Generic;
using System.Linq;

namespace BililiveRecorder.Flv.Pipeline
{
    public class PipelineLogAlternativeHeaderAction : PipelineAction
    {
        public IReadOnlyList<Tag> Tags { get; set; }

        public PipelineLogAlternativeHeaderAction(IReadOnlyList<Tag> tags)
        {
            this.Tags = tags ?? throw new ArgumentNullException(nameof(tags));
        }

        public override PipelineAction Clone() => new PipelineLogAlternativeHeaderAction(this.Tags.ToArray());
    }
}
