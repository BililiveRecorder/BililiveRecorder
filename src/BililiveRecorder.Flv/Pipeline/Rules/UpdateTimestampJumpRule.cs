using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BililiveRecorder.Flv.Pipeline.Actions;
using StructLinq;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 修复时间戳跳变
    /// </summary>
    public class UpdateTimestampJumpRule : ISimpleProcessingRule
    {
        private const string TS_STORE_KEY = "Timestamp_Store_Key";

        private const int JUMP_THRESHOLD = 500;

        private const int AUDIO_DURATION_FALLBACK = 22;
        private const int AUDIO_DURATION_MIN = 20;
        private const int AUDIO_DURATION_MAX = 24;

        private const int VIDEO_DURATION_FALLBACK = 33;
        private const int VIDEO_DURATION_MIN = 15;
        private const int VIDEO_DURATION_MAX = 50;

        public void Run(FlvProcessingContext context, Action next)
        {
            next();

            // 之前直播流的时间戳信息保存在 SessionItems 里
            var ts = context.SessionItems.ContainsKey(TS_STORE_KEY) ? context.SessionItems[TS_STORE_KEY] as TimestampStore ?? new TimestampStore() : new TimestampStore();
            context.SessionItems[TS_STORE_KEY] = ts;

            // 按顺序处理每个 Action
            foreach (var action in context.Actions)
            {
                if (action is PipelineDataAction dataAction) // 如果是直播数据，计算并调整时间戳
                {   // SetDataTimestamp
                    var tags = dataAction.Tags;
                    var currentTimestamp = tags[0].Timestamp;

                    var isFirstChunk = ts.FirstChunk;
                    if (isFirstChunk)
                    {
                        // 第一段数据使用最小的时间戳作为基础偏移量
                        // 防止出现前几个 Tag 时间戳为负数的情况
                        ts.FirstChunk = false;

                        var min = tags.Min(t => t.Timestamp);
                        currentTimestamp = min;
                    }

                    var diff = currentTimestamp - ts.LastOriginal;

                    if (diff < (-JUMP_THRESHOLD) || (isFirstChunk && diff < 0))
                    {
                        context.AddComment(new ProcessingComment(CommentType.TimestampJump, true, $"时间戳变小, curr: {currentTimestamp}, diff: {diff}"));
                        ts.CurrentOffset = currentTimestamp - ts.NextTimestampTarget;
                    }
                    else if (diff > JUMP_THRESHOLD)
                    {
                        context.AddComment(new ProcessingComment(CommentType.TimestampJump, true, $"时间戳间隔过大, curr: {currentTimestamp}, diff: {diff}"));
                        ts.CurrentOffset = currentTimestamp - ts.NextTimestampTarget;
                    }

                    ts.LastOriginal = tags[tags.Count - 1].Timestamp;

                    foreach (var tag in tags)
                        tag.Timestamp -= ts.CurrentOffset;

                    ts.NextTimestampTarget = this.CalculateNewTarget(tags);
                }
                else if (action is PipelineEndAction endAction) // End Tag 其实怎么处理都无所谓
                {
                    var tag = endAction.Tag;
                    var diff = tag.Timestamp - ts.LastOriginal;
                    if (diff is < 0 or > JUMP_THRESHOLD)
                    {
                        tag.Timestamp = ts.NextTimestampTarget;
                    }
                    else
                    {
                        tag.Timestamp -= ts.CurrentOffset;
                    }
                }
                else if (action is PipelineNewFileAction) // 如果新建文件分段了，重设时间戳信息重新从 0 开始
                {
                    ts.Reset();
                }
                else if (action is PipelineScriptAction s) // ~~Script Tag 时间戳永远为 0~~
                {
                    // 这个分支把收到 script tag 时分段禁用了
                    // 所以把时间戳设置成 NextTimestampTarget 而不是固定的 0
                    s.Tag.Timestamp = ts.NextTimestampTarget;
                    // ts.Reset();
                }
                else if (action is PipelineHeaderAction h)
                {
                    var annexBState = HandleNewHeaderRule.GetAnnexBState(context);
                    if (annexBState != HandleNewHeaderRule.AnnexBState.IsAnnexB)
                    {
                        // Header Tag 时间戳重设到 0
                        if (h.VideoHeader != null)
                            h.VideoHeader.Timestamp = 0;
                        if (h.AudioHeader != null)
                            h.AudioHeader.Timestamp = 0;
                    }
                }
            }
        }

        // 计算理想情况下这段数据后面下一个 Tag 的时间戳应该是多少
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CalculateNewTarget(IReadOnlyList<Tag> tags)
        {
            // 有可能出现只有音频或只有视频的情况
            int video = 0, audio = 0;

            if (tags.ToStructEnumerable().Any(ref LinqFunctions.TagIsVideo, x => x))
                video = CalculatePerChannel(tags, VIDEO_DURATION_FALLBACK, VIDEO_DURATION_MAX, VIDEO_DURATION_MIN, TagType.Video);

            if (tags.ToStructEnumerable().Any(ref LinqFunctions.TagIsAudio, x => x))
                audio = CalculatePerChannel(tags, AUDIO_DURATION_FALLBACK, AUDIO_DURATION_MAX, AUDIO_DURATION_MIN, TagType.Audio);

            return Math.Max(video, audio);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int CalculatePerChannel(IReadOnlyList<Tag> tags, int fallback, int max, int min, TagType type)
            {
                var sample = tags.ToStructEnumerable().Where(x => x.Type == type).Take(2).ToArray();
                int durationPerTag;
                if (sample.Length != 2)
                {
                    durationPerTag = fallback;
                }
                else
                {
                    durationPerTag = sample[1].Timestamp - sample[0].Timestamp;

                    if (durationPerTag < min || durationPerTag > max)
                        durationPerTag = fallback;
                }

                return durationPerTag + tags.ToStructEnumerable().Last(x => x.Type == type).Timestamp;
            }
        }

        private class TimestampStore
        {
            public TimestampStore()
            {
                this.Reset();
            }

            /// <summary>
            /// 第一块直播数据特殊处理
            /// </summary>
            public bool FirstChunk;

            /// <summary>
            /// 下一个 Tag 的目标时间戳
            /// </summary>
            public int NextTimestampTarget;

            /// <summary>
            /// 上一个 Tag 的原始时间戳
            /// </summary>
            public int LastOriginal;

            /// <summary>
            /// 当前时间戳偏移量
            /// </summary>
            public int CurrentOffset;

            public void Reset()
            {
                this.FirstChunk = true;
                this.NextTimestampTarget = 0;
                this.LastOriginal = 0;
                this.CurrentOffset = 0;
            }
        }
    }
}
