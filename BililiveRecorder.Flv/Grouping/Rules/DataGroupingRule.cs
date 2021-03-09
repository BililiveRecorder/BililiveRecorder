using System;
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
                var lastAudio = tags.LastOrDefault(x => x.Type == TagType.Audio);
                bool predicate(Tag x) => x.Type == TagType.Audio && x.Timestamp >= ts;

                if (tag.IsKeyframeData())
                {
                    if (lastAudio is not null && Math.Abs(tag.Timestamp - lastAudio.Timestamp) <= 50 && tags.Any(predicate))
                    {
                        leftover = new List<Tag>();
                        leftover.AddRange(tags.Where(predicate));
                        leftover.Add(tag);
                        tags.RemoveAll(predicate);
                        return false;
                    }
                }

                leftover = new List<Tag> { tag };
                return false;
            }
        }

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineDataAction(tags);
    }
}
