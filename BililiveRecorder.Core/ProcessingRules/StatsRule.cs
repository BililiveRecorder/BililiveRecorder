using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Core.ProcessingRules
{
    public class StatsRule : ISimpleProcessingRule
    {
        public event EventHandler<RecordingStatsEventArgs>? StatsUpdated;

        public long TotalInputVideoByteCount { get; private set; }
        public long TotalInputAudioByteCount { get; private set; }
        // 两个值相加可得出总数据量

        public int TotalOutputVideoFrameCount { get; private set; }
        public int TotalOutputAudioFrameCount { get; private set; }
        public long TotalOutputVideoByteCount { get; private set; }
        public long TotalOutputAudioByteCount { get; private set; }

        public int SumOfMaxTimestampOfClosedFiles { get; private set; }
        public int CurrentFileMaxTimestamp { get; private set; }

        public DateTimeOffset LastWriteTime { get; private set; }

        public async Task RunAsync(FlvProcessingContext context, Func<Task> next)
        {
            var e = new RecordingStatsEventArgs();

            if (context.OriginalInput is PipelineDataAction data)
            {
                e.TotalInputVideoByteCount = this.TotalInputVideoByteCount += e.InputVideoByteCount = data.Tags.Where(x => x.Type == TagType.Video).Sum(x => x.Size + (11 + 4));
                e.TotalInputAudioByteCount = this.TotalInputAudioByteCount += e.InputAudioByteCount = data.Tags.Where(x => x.Type == TagType.Audio).Sum(x => x.Size + (11 + 4));
            }

            await next().ConfigureAwait(false);

            var groups = new List<List<PipelineDataAction>?>();
            {
                List<PipelineDataAction>? curr = null;
                foreach (var action in context.Output)
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

            var maxTimestampBeforeCalc = this.CurrentFileMaxTimestamp;

            foreach (var item in groups)
            {
                if (item is null)
                    NewFile();
                else
                    CalcStats(e, item);
            }

            e.AddedDuration = (this.CurrentFileMaxTimestamp - maxTimestampBeforeCalc) / 1000d;
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

                    var lastTags = dataActions[dataActions.Count - 1].Tags;
                    if (lastTags.Count > 0)
                        this.CurrentFileMaxTimestamp = e.FileMaxTimestamp = lastTags[lastTags.Count - 1].Timestamp;

                }

                e.SessionMaxTimestamp = this.SumOfMaxTimestampOfClosedFiles + this.CurrentFileMaxTimestamp;
            }

            void NewFile()
            {
                this.SumOfMaxTimestampOfClosedFiles += this.CurrentFileMaxTimestamp;
                this.CurrentFileMaxTimestamp = 0;
            }
        }
    }
}
