using System.Collections.Generic;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class DataGroupingRule : IGroupingRule
    {
        public bool StartWith(Tag tag) => tag.IsData();

        public bool AppendWith(Tag tag, List<Tag> tags) => tag.IsNonKeyframeData()
            || (tag.Type == TagType.Audio && tag.Flag == TagFlag.Header && tags.TrueForAll(x => x.Type != TagType.Audio));

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineDataAction(tags);
    }
}
