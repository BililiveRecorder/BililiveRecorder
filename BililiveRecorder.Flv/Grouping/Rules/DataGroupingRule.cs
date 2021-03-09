using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class DataGroupingRule : IGroupingRule
    {
        public bool StartWith(List<Tag> tags) => tags.Count > 0 && tags[0].IsData();

        public bool AppendWith(Tag tag, List<Tag> tags, out List<Tag>? leftover)
        {
            var flag = tag.IsNonKeyframeData() || (tag.Type == TagType.Audio && tag.Flag == TagFlag.Header && tags.TrueForAll(x => x.Type != TagType.Audio));

            if (flag)
            {
                tags.Add(tag);
                leftover = null;
                return true;
            }
            else
            {
                var ts = tag.Timestamp;
                bool predicate(Tag x) => x.Timestamp > ts;
                if (tag.IsKeyframeData() && ((tag.Timestamp - tags[tags.Count - 1].Timestamp) <= 50) && tags.Any(predicate))
                {
                    leftover = new List<Tag>();
                    leftover.AddRange(tags.Where(predicate));
                    tags.RemoveAll(predicate);
                    return false;
                }
                else
                {
                    // fast path
                    leftover = new List<Tag> { tag };
                    return false;
                }
            }
        }

        public bool StartWith(Tag tag) => tag.IsData();

        public bool AppendWith(Tag tag, List<Tag> tags) => tag.IsNonKeyframeData()
            || (tag.Type == TagType.Audio && tag.Flag == TagFlag.Header && tags.TrueForAll(x => x.Type != TagType.Audio));

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineDataAction(tags);
    }
}
