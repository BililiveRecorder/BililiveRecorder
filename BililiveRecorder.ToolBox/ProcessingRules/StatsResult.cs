using System.Collections.Generic;

namespace BililiveRecorder.ToolBox.ProcessingRules
{
    public class StatsResult
    {
        public StatsResult(FlvStats video, FlvStats audio, Dictionary<int, int> tagCompositionTimes, Dictionary<int, int> gopMinCompositionTimes)
        {
            this.Video = video;
            this.Audio = audio;
            this.TagCompositionTimes = tagCompositionTimes;
            this.GopMinCompositionTimes = gopMinCompositionTimes;
        }

        public FlvStats Video { get; set; }
        public FlvStats Audio { get; set; }

        public Dictionary<int, int> TagCompositionTimes { get; set; }
        public Dictionary<int, int> GopMinCompositionTimes { get; set; }
    }
}
