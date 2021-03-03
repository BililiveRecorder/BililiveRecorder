using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    public class UpdateTimestampRule : ISimpleProcessingRule
    {
        public const string TS_STORE_KEY = "Timestamp_Store_Key";

        private const int JUMP_THRESHOLD = 50;

        private const int AUDIO_DURATION_FALLBACK = 22;
        private const int AUDIO_DURATION_MIN = 20;
        private const int AUDIO_DURATION_MAX = 24;

        private const int VIDEO_DURATION_FALLBACK = 33;
        private const int VIDEO_DURATION_MIN = 15;
        private const int VIDEO_DURATION_MAX = 50;

        public async Task RunAsync(FlvProcessingContext context, Func<Task> next)
        {
            await next();

            var ts = context.SessionItems.ContainsKey(TS_STORE_KEY) ? context.SessionItems[TS_STORE_KEY] as TimestampStore ?? new TimestampStore() : new TimestampStore();
            context.SessionItems[TS_STORE_KEY] = ts;

            foreach (var action in context.Output)
            {
                if (action is PipelineDataAction dataAction)
                {
                    this.SetDataTimestamp(dataAction.Tags, ts, context);
                }
                else if (action is PipelineNewFileAction)
                {
                    ts.Reset();
                }
                else if (action is PipelineScriptAction s)
                {
                    s.Tag.Timestamp = 0;
                    ts.Reset();
                }
                else if (action is PipelineHeaderAction h)
                {
                    if (h.VideoHeader != null)
                        h.VideoHeader.Timestamp = 0;
                    if (h.AudioHeader != null)
                        h.AudioHeader.Timestamp = 0;
                    ts.Reset();
                }
            }
        }

        private static readonly ProcessingComment SkippingComment = new ProcessingComment(CommentType.TimestampJump, "未检测到音频数据，跳过时间戳修改", skipCounting: true);

        private void SetDataTimestamp(IList<Tag> tags, TimestampStore ts, FlvProcessingContext context)
        {
            // 检查有至少一个音频数据
            // 在 CheckMissingKeyframeRule 已经确认了有视频数据不需要重复检查
            if (!tags.Any(x => x.Type == TagType.Audio))
            {
                context.AddComment(SkippingComment);
                return;
            }

            var diff = tags[0].Timestamp - ts.LastOriginal;
            if (diff < 0)
            {
                var offsetDiff = this.GetOffsetDiff(tags, ts);
                context.AddComment(new ProcessingComment(CommentType.TimestampJump, "时间戳问题：变小, Offset Diff: " + offsetDiff));
                ts.CurrentOffset += offsetDiff;
            }
            else if (diff > JUMP_THRESHOLD)
            {
                var offsetDiff = this.GetOffsetDiff(tags, ts);
                context.AddComment(new ProcessingComment(CommentType.TimestampJump, "时间戳问题：间隔过大, Offset Diff: " + offsetDiff));
                ts.CurrentOffset += offsetDiff;
            }

            ts.LastVideoOriginal = tags.Last(x => x.Type == TagType.Video).Timestamp;
            ts.LastAudioOriginal = tags.Last(x => x.Type == TagType.Audio).Timestamp;
            ts.LastOriginal = Math.Max(ts.LastVideoOriginal, ts.LastAudioOriginal);

            foreach (var tag in tags)
                tag.Timestamp -= ts.CurrentOffset;
        }

        private int GetOffsetDiff(IList<Tag> tags, TimestampStore ts)
        {
            var videoDiff = this.GetAudioOrVideoOffsetDiff(tags.Where(x => x.Type == TagType.Video).Take(2).ToArray(),
               ts.LastVideoOriginal, t => t >= VIDEO_DURATION_MIN && t <= VIDEO_DURATION_MAX, VIDEO_DURATION_FALLBACK);

            var audioDiff = this.GetAudioOrVideoOffsetDiff(tags.Where(x => x.Type == TagType.Audio).Take(2).ToArray(),
               ts.LastAudioOriginal, t => t >= AUDIO_DURATION_MIN && t <= AUDIO_DURATION_MAX, AUDIO_DURATION_FALLBACK);

            return Math.Min(videoDiff, audioDiff);
        }

        private int GetAudioOrVideoOffsetDiff(Tag[] sample, int lastTimestamp, Func<int, bool> validFunc, int fallbackDuration)
        {
            if (sample.Length <= 1)
                return sample[0].Timestamp - lastTimestamp - fallbackDuration;

            var duration = sample[1].Timestamp - sample[0].Timestamp;

            var valid = validFunc(duration);

            if (!valid)
                duration = fallbackDuration;

            return sample[0].Timestamp - lastTimestamp - duration;
        }

        public class TimestampStore
        {
            public int LastOriginal;

            public int LastVideoOriginal;

            public int LastAudioOriginal;

            public int CurrentOffset;

            public void Reset()
            {
                this.LastOriginal = 0;
                this.LastVideoOriginal = 0;
                this.LastAudioOriginal = 0;
                this.CurrentOffset = 0;
            }
        }
    }
}
