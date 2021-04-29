using System;
using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Pipeline.Actions;
using StructLinq;

namespace BililiveRecorder.ToolBox.ProcessingRules
{
    public class StatsRule : ISimpleProcessingRule
    {
        private readonly List<int> audioTimestamp = new List<int>();
        private readonly List<int> videoTimestamp = new List<int>();

        public void Run(FlvProcessingContext context, Action next)
        {
            foreach (var action in context.Actions)
            {
                if (action is PipelineDataAction data)
                {
                    foreach (var tag in data.Tags)
                    {
                        if (tag.Type == TagType.Video)
                        {
                            this.videoTimestamp.Add(tag.Timestamp);
                        }
                        else if (tag.Type == TagType.Audio && tag.Flag == TagFlag.None)
                        {
                            this.audioTimestamp.Add(tag.Timestamp);
                        }
                    }
                }
            }
            next();
        }

        public (FlvStats video, FlvStats audio) GetStats() => (this.CalculateOne(this.videoTimestamp), this.CalculateOne(this.audioTimestamp));

        public FlvStats CalculateOne(List<int> timestamps)
        {
            var stat = new FlvStats
            {
                FrameCount = timestamps.Count,
                FrameDurations = timestamps
                .Select((time, i) => i == 0 ? 0 : time - timestamps[i - 1])
                .Skip(1)
                .GroupBy(x => x)
                .ToDictionary(x => x.Key, x => x.Count())
                .OrderByDescending(x => x.Value)
                .ThenByDescending(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value)
            };
            stat.FramePerSecond = 1000d / stat.FrameDurations.Select(x => x.Key * ((double)x.Value / timestamps.Count)).Sum();

            return stat;
        }
    }
}
