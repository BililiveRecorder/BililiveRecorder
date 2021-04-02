using System.Collections.Generic;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class ScriptGroupingRule : IGroupingRule
    {
        public bool StartWith(Tag tag) => tag.IsScript();

        public bool AppendWith(Tag tag, LinkedList<Tag> tags, out LinkedList<Tag>? leftover)
        {
            leftover = new LinkedList<Tag>();
            leftover.AddLast(tag);
            return false;
        }

        public PipelineAction CreatePipelineAction(LinkedList<Tag> tags) => new PipelineScriptAction(tags.First.Value);
    }
}
