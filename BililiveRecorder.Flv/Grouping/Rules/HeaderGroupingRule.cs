using System.Collections.Generic;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class HeaderGroupingRule : IGroupingRule
    {
        public bool StartWith(Tag tag) => tag.IsHeader();

        public bool AppendWith(Tag tag, List<Tag> tags) => tag.IsHeader();

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineHeaderAction(tags);
    }
}
