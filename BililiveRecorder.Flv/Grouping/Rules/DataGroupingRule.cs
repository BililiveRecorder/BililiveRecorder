using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv.Pipeline.Actions;
using StructLinq;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class DataGroupingRule : IGroupingRule
    {
        private readonly struct DoesNotContainAudioData : IFunction<Tag, bool>
        {
            public static DoesNotContainAudioData Instance;
            public bool Eval(Tag element) => element.Type != TagType.Audio || element.Flag == TagFlag.Header;
        }

        public bool CanStartWith(Tag tag) => tag.IsData();

        public bool CanAppendWith(Tag tag, List<Tag> tags) =>
            // Tag 是非关键帧数据
            tag.IsNonKeyframeData()
            // 或是音频头，并且之前未出现过音频数据
            || (tag.Type == TagType.Audio && tag.Flag == TagFlag.Header && tags.ToStructEnumerable().All(ref DoesNotContainAudioData.Instance, x => x));
        // || (tag.IsKeyframeData() && tags.All(x => x.IsNonKeyframeData()))

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineDataAction(tags);
    }
}
