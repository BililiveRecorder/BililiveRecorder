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
    internal class StatsRule : ISimpleProcessingRule
    {
        public const string SkipStatsKey = nameof(SkipStatsKey);

        public StatsRule(DateTimeOffset? RecordingStart = null)
        {
            this.RecordingStart = RecordingStart ?? DateTimeOffset.Now;
        }

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
        public DateTimeOffset RecordingStart { get; }

        public void Run(FlvProcessingContext context, Action next)
        {
            var e = new RecordingStatsEventArgs
            {
                SessionDuration = (DateTimeOffset.Now - this.RecordingStart).TotalMilliseconds
            };

            {
                static IEnumerable<PipelineDataAction> FilterDataActions(IEnumerable<PipelineAction> actions)
                {
                    foreach (var action in actions)
                        if (action is PipelineDataAction dataAction)
                            yield return dataAction;
                }

                e.TotalInputVideoBytes = this.TotalInputVideoByteCount += e.InputVideoBytes =
                    FilterDataActions(context.Actions).ToStructEnumerable().Sum(ref LinqFunctions.SumSizeOfVideoData, x => x, x => x);

                e.TotalInputAudioBytes = this.TotalInputAudioByteCount += e.InputAudioBytes =
                    FilterDataActions(context.Actions).ToStructEnumerable().Sum(ref LinqFunctions.SumSizeOfAudioData, x => x, x => x);

                e.TotalInputBytes = e.TotalInputVideoBytes + e.TotalInputAudioBytes;
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
            e.PassedTime = (now - this.LastWriteTime).TotalMilliseconds;
            this.LastWriteTime = now;
            e.DurationRatio = e.AddedDuration / e.PassedTime;

            StatsUpdated?.Invoke(this, e);

            return;
            void CalcStats(RecordingStatsEventArgs e, IReadOnlyList<PipelineDataAction> dataActions)
            {
                if (dataActions.Count > 0)
                {
                    e.TotalOutputVideoFrames = this.TotalOutputVideoFrameCount += e.OutputVideoFrames =
                        dataActions.ToStructEnumerable().Sum(ref LinqFunctions.CountVideoTags, x => x, x => x);

                    e.TotalOutputAudioFrames = this.TotalOutputAudioFrameCount += e.OutputAudioFrames =
                        dataActions.ToStructEnumerable().Sum(ref LinqFunctions.CountAudioTags, x => x, x => x);

                    e.TotalOutputVideoBytes = this.TotalOutputVideoByteCount += e.OutputVideoBytes =
                        dataActions.ToStructEnumerable().Sum(ref LinqFunctions.SumSizeOfVideoDataByNalu, x => x, x => x);

                    e.TotalOutputAudioBytes = this.TotalOutputAudioByteCount += e.OutputAudioBytes =
                        dataActions.ToStructEnumerable().Sum(ref LinqFunctions.SumSizeOfAudioData, x => x, x => x);

                    e.TotalOutputBytes = e.TotalOutputAudioBytes + e.TotalOutputVideoBytes;

                    e.CurrentFileSize = this.CurrentFileSize += e.OutputVideoBytes + e.OutputAudioBytes;

                    foreach (var action in dataActions)
                    {
                        var tags = action.Tags;
                        if (tags.Count > 0)
                        {
                            e.AddedDuration += (tags[tags.Count - 1].Timestamp - tags[0].Timestamp);
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
