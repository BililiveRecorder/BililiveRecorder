using System.Collections.Generic;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv
{
    public interface IGroupingRule
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tags">Current Tags</param>
        /// <returns></returns>
        bool StartWith(List<Tag> tags);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tag">Tag not yet added to the list</param>
        /// <param name="tags">List of tags</param>
        /// <param name="leftover"></param>
        /// <returns></returns>
        bool AppendWith(Tag tag, List<Tag> tags, out List<Tag>? leftover);

        PipelineAction CreatePipelineAction(List<Tag> tags);
    }
}
