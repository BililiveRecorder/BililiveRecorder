using System;
using System.Collections.Generic;
using System.Linq;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    public class UpdateTimestampOffsetRule : ISimpleProcessingRule
    {
        private static readonly ProcessingComment comment1 = new ProcessingComment(CommentType.Unrepairable, "GOP 内音频或视频时间戳不连续");
        private static readonly ProcessingComment comment2 = new ProcessingComment(CommentType.Unrepairable, "出现了无法计算偏移量的音视频偏移");

        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private bool CheckIfNormal(IEnumerable<Tag> data) => !data.Any2((a, b) => a.Timestamp > b.Timestamp);

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            if (action is PipelineDataAction data)
            {
                var isNormal = this.CheckIfNormal(data.Tags);
                if (isNormal)
                {
                    yield return data;
                    yield break;
                }

                if (!(this.CheckIfNormal(data.Tags.Where(x => x.Type == TagType.Audio)) && this.CheckIfNormal(data.Tags.Where(x => x.Type == TagType.Video))))
                {
                    // 音频或视频自身就有问题，没救了
                    yield return PipelineDisconnectAction.Instance;
                    context.AddComment(comment1);
                    yield break;
                }
                else
                {
                    var oc = new OffsetCalculator();

                    foreach (var tag in data.Tags)
                        oc.AddTag(tag);

                    if (oc.Calculate(out var videoOffset))
                    {
                        if (videoOffset != 0)
                        {
                            context.AddComment(new ProcessingComment(CommentType.TimestampOffset, $"音视频时间戳偏移, D: {videoOffset}"));

                            foreach (var tag in data.Tags)
                                if (tag.Type == TagType.Video)
                                    tag.Timestamp += videoOffset;
                        }

                        yield return data;
                        yield break;
                    }
                    else
                    {
                        context.AddComment(comment2); 
                        yield return PipelineDisconnectAction.Instance; 
                        yield break;
                    }
                }
            }
            else
                yield return action;
        }

        /// <summary>
        /// 音视频偏差量计算
        /// </summary>
        private class OffsetCalculator
        {
            /*      算法思路和原理
             * 设定作调整的为视频帧，参照每个视频帧左右（左为前、右为后）的音频帧的时间戳
             * 计算出最多和最少能符合“不小于前面的帧并且不大于后面的帧”的要求的偏移量
             * 如果当前偏移量比总偏移量要求更严，则使用当前偏移量范围作为总偏移量范围
             * */

            private Tag? lastAudio = null;
            private readonly Stack<Tag> tags = new Stack<Tag>();

            private int maxOffset = int.MaxValue;
            private int minOffset = int.MinValue;

            public void AddTag(Tag tag)
            {
                if (tag.Type == TagType.Audio)
                {
                    this.ReduceOffsetRange(this.lastAudio, tag);
                    this.lastAudio = tag;
                }
                else if (tag.Type == TagType.Video)
                {
                    this.tags.Push(tag);
                }
                else
                    throw new ArgumentException("unexpected tag type");
            }

            public bool Calculate(out int offset)
            {
                this.ReduceOffsetRange(this.lastAudio, null);
                this.lastAudio = null;

                if (this.minOffset == this.maxOffset)
                {
                    offset = this.minOffset;
                    return true;
                }
                else if (this.minOffset <= this.maxOffset)
                {
                    offset = (this.minOffset + this.maxOffset) / 2;
                    return true;
                }
                else
                {
                    offset = 0;
                    return false;
                }
            }

            private void ReduceOffsetRange(Tag? leftAudio, Tag? rightAudio)
            {
                while (this.tags.Count > 0)
                {
                    var video = this.tags.Pop();

                    if (leftAudio is not null)
                    {
                        var min = leftAudio.Timestamp - video.Timestamp;
                        if (this.minOffset < min)
                            this.minOffset = min;
                    }

                    if (rightAudio is not null)
                    {
                        var max = rightAudio.Timestamp - video.Timestamp;
                        if (this.maxOffset > max)
                            this.maxOffset = max;
                    }
                }
            }
        }
    }
}
