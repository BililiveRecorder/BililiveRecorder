using System;

namespace BililiveRecorder.Core.Event
{
    public class RecordingStatsEventArgs : EventArgs
    {
        public long InputVideoByteCount { get; set; }
        public long InputAudioByteCount { get; set; }

        public int OutputVideoFrameCount { get; set; }
        public int OutputAudioFrameCount { get; set; }
        public long OutputVideoByteCount { get; set; }
        public long OutputAudioByteCount { get; set; }

        public long TotalInputVideoByteCount { get; set; }
        public long TotalInputAudioByteCount { get; set; }

        public int TotalOutputVideoFrameCount { get; set; }
        public int TotalOutputAudioFrameCount { get; set; }
        public long TotalOutputVideoByteCount { get; set; }
        public long TotalOutputAudioByteCount { get; set; }

        public double AddedDuration { get; set; }
        public double PassedTime { get; set; }
        public double DuraionRatio { get; set; }
        public int SessionMaxTimestamp { get; set; }
        public int FileMaxTimestamp { get; set; }
    }
}
