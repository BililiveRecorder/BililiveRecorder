using System.Collections.Generic;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv
{
    public interface IGroupingRule
    {
        bool StartWith(Tag tag);
        bool AppendWith(Tag tag, List<Tag> tags);
        PipelineAction CreatePipelineAction(List<Tag> tags);
    }
}
