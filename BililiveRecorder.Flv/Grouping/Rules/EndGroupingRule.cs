using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class EndGroupingRule : IGroupingRule
    {
        public bool StartWith(Tag tag) => tag.IsEnd();

        public bool AppendWith(Tag tag, List<Tag> tags) => false;

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineEndAction(tags.First());
    }
}
