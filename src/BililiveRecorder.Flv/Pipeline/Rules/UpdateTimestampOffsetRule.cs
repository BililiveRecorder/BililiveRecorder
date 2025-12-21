using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BililiveRecorder.Flv.Pipeline.Actions;
using StructLinq;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 修复时间戳错位
    /// </summary>
    public class UpdateTimestampOffsetRule : ISimpleProcessingRule
    {
        private static readonly ProcessingComment COMMENT_JumpedWithinGOP = new ProcessingComment(CommentType.Unrepairable, true, "GOP 内音频或视频时间戳不连续");
        private static readonly ProcessingComment COMMENT_CantSolve = new ProcessingComment(CommentType.Unrepairable, true, "出现了无法计算偏移量的音视频偏移");

        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        /// <summary>
        /// 检查 Tag 时间戳是否有变小的情况
        /// </summary>
        private readonly struct IsNextTimestampSmaller : ITwoInputFunction<Tag, bool>
        {
            public static IsNextTimestampSmaller Instance;
            public bool Eval(Tag a, Tag b) => a.Timestamp > b.Timestamp;
        }

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            if (action is PipelineDataAction data)
            {
                var isNotNormal = data.Tags.Any2(ref IsNextTimestampSmaller.Instance);
                if (!isNotNormal)
                {
                    // 没有问题，每个 tag 的时间戳都 >= 上一个 tag 的时间戳。
                    yield return data;
                    yield break;
                }

                if (data.Tags.Where(x => x.Type == TagType.Audio).Any2(ref IsNextTimestampSmaller.Instance) || data.Tags.Where(x => x.Type == TagType.Video).Any2(ref IsNextTimestampSmaller.Instance))
                {
                    // 音频或视频自身就有问题，没救了
                    yield return new PipelineDisconnectAction("GOP 内音频或视频时间戳不连续");
                    context.AddComment(COMMENT_JumpedWithinGOP);
                    yield break;
                }
                else
                {
                    /*
                     * 设定做调整的为视频帧，参照每个视频帧左右（左为前、右为后）的音频帧的时间戳
                     * 计算出最多和最少能符合“不小于前面的帧并且不大于后面的帧”的要求的偏移量
                     * 如果当前偏移量比总偏移量要求更严，则使用当前偏移量范围作为总偏移量范围
                     * */
                    var offset = 0;

                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    static void ReduceOffsetRange(ref int maxOffset, ref int minOffset, Tag? leftAudio, Tag? rightAudio, Stack<Tag> tags)
                    {
                        while (tags.Count > 0)
                        {
                            var video = tags.Pop();

                            if (leftAudio is not null)
                            {
                                var min = leftAudio.Timestamp - video.Timestamp;
                                if (minOffset < min)
                                    minOffset = min;
                            }

                            if (rightAudio is not null)
                            {
                                var max = rightAudio.Timestamp - video.Timestamp;
                                if (maxOffset > max)
                                    maxOffset = max;
                            }
                        }
                    }

                    Tag? lastAudio = null;
                    var tags = new Stack<Tag>();
                    var maxOffset = int.MaxValue;
                    var minOffset = int.MinValue;

                    for (var i = 0; i < data.Tags.Count; i++)
                    {
                        var tag = data.Tags[i];
                        if (tag.Type == TagType.Audio)
                        {
                            ReduceOffsetRange(ref maxOffset, ref minOffset, lastAudio, tag, tags);
                            lastAudio = tag;
                        }
                        else if (tag.Type == TagType.Video)
                        {
                            tags.Push(tag);
                        }
                        else
                            throw new ArgumentException("unexpected tag type");
                    }

                    ReduceOffsetRange(ref maxOffset, ref minOffset, lastAudio, null, tags);

                    if (minOffset == maxOffset)
                    {   // 理想情况允许偏移范围只有一个值
                        offset = minOffset;
                        goto validOffset;
                    }
                    else if (minOffset < maxOffset)
                    {   // 允许偏移的值是一个范围
                        if (minOffset != int.MinValue)
                        {
                            if (maxOffset != int.MaxValue)
                            {   // 有一个有效范围，取平均值
                                offset = (int)(((long)minOffset + maxOffset) / 2L);
                                goto validOffset;
                            }
                            else
                            {   // 无效最大偏移，以最小偏移为准
                                offset = minOffset + 1;
                                goto validOffset;
                            }
                        }
                        else
                        {
                            if (maxOffset != int.MaxValue)
                            {   // 无效最小偏移，以最大偏移为准
                                offset = maxOffset - 1;
                                goto validOffset;
                            }
                            else
                            {   // 无效结果
                                goto invalidOffset;
                            }
                        }
                    }
                    else
                    {   // 范围无效
                        goto invalidOffset;
                    }

                validOffset:
                    if (offset != 0)
                    {
                        context.AddComment(new ProcessingComment(CommentType.TimestampOffset, true, $"音视频时间戳偏移, D: {offset}"));

                        foreach (var tag in data.Tags)
                            if (tag.Type == TagType.Video)
                                tag.Timestamp += offset;
                    }

                    yield return data;
                    yield break;

                invalidOffset:
                    context.AddComment(COMMENT_CantSolve);
                    yield return new PipelineDisconnectAction("出现无法计算的音视频时间戳错位");
                    yield break;
                }
            }
            else
                yield return action;
        }
    }
}
