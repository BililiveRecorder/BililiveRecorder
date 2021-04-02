using System;
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
            var flag = tag.IsNonKeyframeData()
                        || (tag.IsKeyframeData() && tags.All(x => x.IsNonKeyframeData()))
                        || (tag.Type == TagType.Audio && tag.Flag == TagFlag.Header && tags.All(x => x.Type != TagType.Audio || x.Flag == TagFlag.Header));

            if (flag)
            {
                tags.AddLast(tag);
                leftover = null;
                return true;
            }
            else
            {
                var ts = tag.Timestamp;
                var lastAudio = tags.LastOrDefault(x => x.Type == TagType.Audio);

                bool predicate(Tag x) => x.Type == TagType.Audio && x.Timestamp >= ts;

                if (tag.IsKeyframeData() && lastAudio is not null && Math.Abs(tag.Timestamp - lastAudio.Timestamp) <= 50 && tags.Any(predicate))
                {
                    {
                        leftover = new LinkedList<Tag>();
                        foreach (var item in tags.Where(predicate))
                            leftover.AddLast(item);
                        leftover.AddLast(tag);
                    }

                    // tags.RemoveAll(predicate);
                    {
                        var node = tags.First;
                        while (node != null)
                        {
                            var next = node.Next;
                            if (predicate(node.Value))
                                tags.Remove(node);
                            node = next;
                        }
                    }

                    return false;
                }
                else
                {
                    leftover = new LinkedList<Tag>();
                    leftover.AddLast(tag);
                    return false;
                }
            }
        }

        public PipelineAction CreatePipelineAction(LinkedList<Tag> tags) => new PipelineDataAction(new List<Tag>(tags));
    }
}
