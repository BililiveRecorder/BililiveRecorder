using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class DataGroupingRule : IGroupingRule
    {
        public bool StartWith(Tag tag) => tag.IsData();

        public bool AppendWith(Tag tag, LinkedList<Tag> tags, out LinkedList<Tag>? leftover)
        {
            var shouldAppend =
                // Tag 是非关键帧数据
                tag.IsNonKeyframeData()
                // 或是音频头，并且之前未出现过音频数据
                || (tag.Type == TagType.Audio && tag.Flag == TagFlag.Header && tags.All(x => x.Type != TagType.Audio || x.Flag == TagFlag.Header));
            // || (tag.IsKeyframeData() && tags.All(x => x.IsNonKeyframeData()))

            if (shouldAppend)
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

        public PipelineAction CreatePipelineAction(LinkedList<Tag> tags) => new PipelineDataAction(new List<Tag>(tags));
    }
}
