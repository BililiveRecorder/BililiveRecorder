using System;
using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Core.ProcessingRules
{
    public class StatsRule : ISimpleProcessingRule
    {
        public const string SkipStatsKey = nameof(SkipStatsKey);

        public event EventHandler<RecordingStatsEventArgs>? StatsUpdated;

        public long TotalInputVideoByteCount { get; private set; }
        public long TotalInputAudioByteCount { get; private set; }

        public int TotalOutputVideoFrameCount { get; private set; }
        public int TotalOutputAudioFrameCount { get; private set; }
        public long TotalOutputVideoByteCount { get; private set; }
        public long TotalOutputAudioByteCount { get; private set; }

        public long CurrnetFileSize { get; private set; } = 13;

        public int SumOfMaxTimestampOfClosedFiles { get; private set; }
        public int CurrentFileMaxTimestamp { get; private set; }

        public DateTimeOffset LastWriteTime { get; private set; }

        public void Run(FlvProcessingContext context, Action next)
        {
            var e = new RecordingStatsEventArgs();

            e.TotalInputVideoByteCount = this.TotalInputVideoByteCount += e.InputVideoByteCount =
                context.Actions.Where(x => x is PipelineDataAction).Cast<PipelineDataAction>().Sum(data => data.Tags.Where(x => x.Type == TagType.Video).Sum(x => x.Size + (11 + 4)));
            e.TotalInputAudioByteCount = this.TotalInputAudioByteCount += e.InputAudioByteCount =
                context.Actions.Where(x => x is PipelineDataAction).Cast<PipelineDataAction>().Sum(data => data.Tags.Where(x => x.Type == TagType.Audio).Sum(x => x.Size + (11 + 4)));

            next();

            var groups = new List<List<PipelineDataAction>?>();
            {
                List<PipelineDataAction>? curr = null;
                foreach (var action in context.Actions)
                {
                    if (action is PipelineDataAction dataAction)
                    {
                        if (curr is null)
                        {
                            curr = new List<PipelineDataAction>();
                            groups.Add(curr);
                        }
                        curr.Add(dataAction);
                    }
                    else if (action is PipelineNewFileAction)
                    {
                        curr = null;
                        groups.Add(null);
                    }
                }
            }

            foreach (var item in groups)
            {
                if (item is null)
                    NewFile();
                else
                    CalcStats(e, item);
            }

            var now = DateTimeOffset.UtcNow;
            e.PassedTime = (now - this.LastWriteTime).TotalSeconds;
            this.LastWriteTime = now;
            e.DuraionRatio = e.AddedDuration / e.PassedTime;

            StatsUpdated?.Invoke(this, e);

            return;
            void CalcStats(RecordingStatsEventArgs e, IReadOnlyList<PipelineDataAction> dataActions)
            {
                if (dataActions.Count > 0)
                {
                    e.TotalOutputVideoFrameCount = this.TotalOutputVideoFrameCount += e.OutputVideoFrameCount = dataActions.Sum(x => x.Tags.Count(x => x.Type == TagType.Video));
                    e.TotalOutputAudioFrameCount = this.TotalOutputAudioFrameCount += e.OutputAudioFrameCount = dataActions.Sum(x => x.Tags.Count(x => x.Type == TagType.Audio));
                    e.TotalOutputVideoByteCount = this.TotalOutputVideoByteCount += e.OutputVideoByteCount = dataActions.Sum(x => x.Tags.Where(x => x.Type == TagType.Video).Sum(x => (x.Nalus == null ? x.Size : (5 + x.Nalus.Sum(n => n.FullSize + 4))) + (11 + 4)));
                    e.TotalOutputAudioByteCount = this.TotalOutputAudioByteCount += e.OutputAudioByteCount = dataActions.Sum(x => x.Tags.Where(x => x.Type == TagType.Audio).Sum(x => x.Size + (11 + 4)));

                    e.CurrnetFileSize = this.CurrnetFileSize += e.OutputVideoByteCount + e.OutputAudioByteCount;

                    foreach (var action in dataActions)
                    {
                        var tags = action.Tags;
                        if (tags.Count > 0)
                        {
                            e.AddedDuration += (tags[tags.Count - 1].Timestamp - tags[0].Timestamp) / 1000d;
                            this.CurrentFileMaxTimestamp = e.FileMaxTimestamp = tags[tags.Count - 1].Timestamp;
                        }
                    }
                }

                e.SessionMaxTimestamp = this.SumOfMaxTimestampOfClosedFiles + this.CurrentFileMaxTimestamp;
            }

            void NewFile()
            {
                this.SumOfMaxTimestampOfClosedFiles += this.CurrentFileMaxTimestamp;
                this.CurrentFileMaxTimestamp = 0;
                this.CurrnetFileSize = 13;
            }
        }
    }
}
