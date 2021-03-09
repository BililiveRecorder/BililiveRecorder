using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class EndGroupingRule : IGroupingRule
    {
        public bool StartWith(List<Tag> tags) => tags.Count == 1 && tags[0].IsEnd();

        public bool AppendWith(Tag tag, List<Tag> tags, out List<Tag>? leftover)
        {
            leftover = new List<Tag> { tag };
            return false;
        }

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineEndAction(tags.First());
    }
}
