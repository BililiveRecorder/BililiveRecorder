using System.Collections.Generic;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv
{
    public interface IGroupingRule
    {
        bool StartWith(Tag tag);
        bool AppendWith(Tag tag);
        PipelineAction CreatePipelineAction(List<Tag> tags);
    }
}
