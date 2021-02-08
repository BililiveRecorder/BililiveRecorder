using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class ScriptGroupingRule : IGroupingRule
    {
        public bool StartWith(Tag tag) => tag.IsScript();

        public bool AppendWith(Tag tag) => false;

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineScriptAction(tags.First());
    }
}
