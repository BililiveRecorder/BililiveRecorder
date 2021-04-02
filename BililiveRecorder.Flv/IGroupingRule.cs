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
        bool StartWith(Tag tag);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tag">Tag not yet added to the list</param>
        /// <param name="tags">List of tags</param>
        /// <param name="leftover"></param>
        /// <returns></returns>
        bool AppendWith(Tag tag, LinkedList<Tag> tags, out LinkedList<Tag>? leftover);

        PipelineAction CreatePipelineAction(LinkedList<Tag> tags);
    }
}
