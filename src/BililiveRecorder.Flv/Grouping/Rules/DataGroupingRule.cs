using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv.Pipeline.Actions;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class DataGroupingRule : IGroupingRule
    {
        public bool CanStartWith(Tag tag) => tag.IsData();

        public bool CanAppendWith(Tag tag, List<Tag> tags) =>
            // Tag 是非关键帧数据，并且与前一个 Tag 的时间戳差距不超过 25 秒
            (tag.IsNonKeyframeData() && (tags.LastOrDefault() is not Tag lastTag || (tag.Timestamp - lastTag.Timestamp < 24999)))
            // 或：是关键帧，并且之前只有音频数据
            || (tag.Type == TagType.Video && tag.IsKeyframeData() && tags.TrueForAll(x => x.Type == TagType.Audio))
            // 或：是音频头，并且之前未出现过音频数据
            || (tag.Type == TagType.Audio && tag.IsHeader() && tags.TrueForAll(x => x.Type != TagType.Audio || x.Flag == TagFlag.Header));
        // || (tag.IsKeyframeData() && tags.All(x => x.IsNonKeyframeData()))

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineDataAction(tags);
    }
}
