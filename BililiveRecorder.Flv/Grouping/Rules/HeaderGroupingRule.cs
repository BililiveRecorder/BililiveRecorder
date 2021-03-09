using System.Collections.Generic;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class HeaderGroupingRule : IGroupingRule
    {
        public bool StartWith(List<Tag> tags) => tags.Count > 0 && tags.TrueForAll(x => x.IsHeader());

        public bool AppendWith(Tag tag, List<Tag> tags, out List<Tag>? leftover)
        {
            if (tag.IsHeader())
            {
                tags.Add(tag);
                leftover = null;
                return true;
            }
            else
            {
                leftover = new List<Tag> { tag };
                return false;
            }
        }

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineHeaderAction(tags);
    }
}
