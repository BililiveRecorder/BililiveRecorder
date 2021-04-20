using System;
using System.Collections.Generic;
using System.Linq;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    public class UpdateTimestampJumpRule : ISimpleProcessingRule
    {
        private const string TS_STORE_KEY = "Timestamp_Store_Key";

        private const int JUMP_THRESHOLD = 50;

        private const int AUDIO_DURATION_FALLBACK = 22;
        private const int AUDIO_DURATION_MIN = 20;
        private const int AUDIO_DURATION_MAX = 24;

        private const int VIDEO_DURATION_FALLBACK = 33;
        private const int VIDEO_DURATION_MIN = 15;
        private const int VIDEO_DURATION_MAX = 50;

        public void Run(FlvProcessingContext context, Action next)
        {
            next();

            var ts = context.SessionItems.ContainsKey(TS_STORE_KEY) ? context.SessionItems[TS_STORE_KEY] as TimestampStore ?? new TimestampStore() : new TimestampStore();
            context.SessionItems[TS_STORE_KEY] = ts;

            foreach (var action in context.Actions)
            {
                if (action is PipelineDataAction dataAction)
                {
                    this.SetDataTimestamp(dataAction.Tags, ts, context);
                }
                else if (action is PipelineEndAction endAction)
                {
                    var tag = endAction.Tag;
                    var diff = tag.Timestamp - ts.LastOriginal;
                    if (diff < 0 || diff > JUMP_THRESHOLD)
                    {
                        tag.Timestamp = ts.NextTimestampTarget;
                    }
                    else
                    {
                        tag.Timestamp -= ts.CurrentOffset;
                    }
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

        private void SetDataTimestamp(IReadOnlyList<Tag> tags, TimestampStore ts, FlvProcessingContext context)
        {
            var currentTimestamp = tags[0].Timestamp;
            var diff = currentTimestamp - ts.LastOriginal;
            if (diff < 0)
            {
                context.AddComment(new ProcessingComment(CommentType.TimestampJump, $"时间戳变小, curr: {currentTimestamp}, diff: {diff}"));
                ts.CurrentOffset = currentTimestamp - ts.NextTimestampTarget;
            }
            else if (diff > JUMP_THRESHOLD)
            {
                context.AddComment(new ProcessingComment(CommentType.TimestampJump, $"时间戳间隔过大, curr: {currentTimestamp}, diff: {diff}"));
                ts.CurrentOffset = currentTimestamp - ts.NextTimestampTarget;
            }

            ts.LastOriginal = tags.Last().Timestamp;

            foreach (var tag in tags)
                tag.Timestamp -= ts.CurrentOffset;

            ts.NextTimestampTarget = this.CalculateNewTarget(tags);
        }

        private int CalculateNewTarget(IReadOnlyList<Tag> tags)
        {
            // 有可能出现只有音频或只有视频的情况
            int video = 0, audio = 0;

            if (tags.Any(x => x.Type == TagType.Video))
                video = CalculatePerChannel(tags, VIDEO_DURATION_FALLBACK, VIDEO_DURATION_MAX, VIDEO_DURATION_MIN, TagType.Video);

            if (tags.Any(x => x.Type == TagType.Audio))
                audio = CalculatePerChannel(tags, AUDIO_DURATION_FALLBACK, AUDIO_DURATION_MAX, AUDIO_DURATION_MIN, TagType.Audio);

            return Math.Max(video, audio);

            static int CalculatePerChannel(IReadOnlyList<Tag> tags, int fallback, int max, int min, TagType type)
            {
                var sample = tags.Where(x => x.Type == type).Take(2).ToArray();
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

                return durationPerTag + tags.Last(x => x.Type == type).Timestamp;
            }
        }

        private class TimestampStore
        {
            public int NextTimestampTarget;

            public int LastOriginal;

            public int CurrentOffset;

            public void Reset()
            {
                this.NextTimestampTarget = 0;
                this.LastOriginal = 0;
                this.CurrentOffset = 0;
            }
        }
    }
}
