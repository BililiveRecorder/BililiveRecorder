using System;
using System.Collections.Generic;
using System.Linq;

namespace BililiveRecorder.Flv.Pipeline.Actions
{
    public class PipelineHeaderAction : PipelineAction
    {
        public PipelineHeaderAction(IReadOnlyList<Tag> allTags)
        {
            this.AllTags = allTags ?? throw new ArgumentNullException(nameof(allTags));
        }

        public Tag? VideoHeader { get; set; }

        public Tag? AudioHeader { get; set; }

        public IReadOnlyList<Tag> AllTags { get; set; }

        public override PipelineAction Clone() => new PipelineHeaderAction(this.AllTags.ToArray())
        {
            VideoHeader = VideoHeader,
            AudioHeader = AudioHeader
        };
    }
}
