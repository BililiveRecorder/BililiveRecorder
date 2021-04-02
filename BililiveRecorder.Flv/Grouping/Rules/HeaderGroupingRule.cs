using System.Collections.Generic;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class HeaderGroupingRule : IGroupingRule
    {
        public bool StartWith(Tag tag) => tag.IsHeader();

        public bool AppendWith(Tag tag, LinkedList<Tag> tags, out LinkedList<Tag>? leftover)
        {
            if (tag.IsHeader())
            {
                tags.AddLast(tag);
                leftover = null;
                return true;
            }
            else
            {
                leftover = new LinkedList<Tag>();
                leftover.AddLast(tag);
                return false;
            }
        }

        public PipelineAction CreatePipelineAction(LinkedList<Tag> tags) => new PipelineHeaderAction(new List<Tag>(tags));
    }
}
