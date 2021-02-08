using System.Collections.Generic;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class DataGroupingRule : IGroupingRule
    {
        public bool StartWith(Tag tag) => tag.IsData();

        public bool AppendWith(Tag tag) => tag.IsNonKeyframeData();

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineDataAction(tags);
    }
}
