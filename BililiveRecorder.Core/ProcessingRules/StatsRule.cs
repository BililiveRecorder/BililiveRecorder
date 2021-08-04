using System;
using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Pipeline.Actions;
using StructLinq;

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

        public long CurrentFileSize { get; private set; } = 13;

        public int SumOfMaxTimestampOfClosedFiles { get; private set; }
        public int CurrentFileMaxTimestamp { get; private set; }

        public DateTimeOffset LastWriteTime { get; private set; }

        public void Run(FlvProcessingContext context, Action next)
        {
            var e = new RecordingStatsEventArgs();

            {
                static IEnumerable<PipelineDataAction> FilterDataActions(IEnumerable<PipelineAction> actions)
                {
                    foreach (var action in actions)
                        if (action is PipelineDataAction dataAction)
                            yield return dataAction;
                }

                e.TotalInputVideoByteCount = this.TotalInputVideoByteCount += e.InputVideoByteCount =
                    FilterDataActions(context.Actions).ToStructEnumerable().Sum(ref LinqFunctions.SumSizeOfVideoData, x => x, x => x);

                e.TotalInputAudioByteCount = this.TotalInputAudioByteCount += e.InputAudioByteCount =
                    FilterDataActions(context.Actions).ToStructEnumerable().Sum(ref LinqFunctions.SumSizeOfAudioData, x => x, x => x);
            }

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
            e.DurationRatio = e.AddedDuration / e.PassedTime;

            StatsUpdated?.Invoke(this, e);

            return;
            void CalcStats(RecordingStatsEventArgs e, IReadOnlyList<PipelineDataAction> dataActions)
            {
                if (dataActions.Count > 0)
                {
                    e.TotalOutputVideoFrameCount = this.TotalOutputVideoFrameCount += e.OutputVideoFrameCount =
                        dataActions.ToStructEnumerable().Sum(ref LinqFunctions.CountVideoTags, x => x, x => x);

                    e.TotalOutputAudioFrameCount = this.TotalOutputAudioFrameCount += e.OutputAudioFrameCount =
                        dataActions.ToStructEnumerable().Sum(ref LinqFunctions.CountAudioTags, x => x, x => x);

                    e.TotalOutputVideoByteCount = this.TotalOutputVideoByteCount += e.OutputVideoByteCount =
                        dataActions.ToStructEnumerable().Sum(ref LinqFunctions.SumSizeOfVideoDataByNalu, x => x, x => x);

                    e.TotalOutputAudioByteCount = this.TotalOutputAudioByteCount += e.OutputAudioByteCount =
                        dataActions.ToStructEnumerable().Sum(ref LinqFunctions.SumSizeOfAudioData, x => x, x => x);

                    e.CurrentFileSize = this.CurrentFileSize += e.OutputVideoByteCount + e.OutputAudioByteCount;

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
                this.CurrentFileSize = 13;
            }
        }
    }
}
