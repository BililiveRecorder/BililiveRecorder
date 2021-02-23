using System;
using Newtonsoft.Json;

namespace BililiveRecorder.Core.Event
{
    public class RecordFileClosedEventArgs : RecordEventArgsBase
    {
        public RecordFileClosedEventArgs() { }

        public RecordFileClosedEventArgs(IRoom room) : base(room) { }

        [JsonIgnore]
        public string FullPath { get; set; } = string.Empty;

        public string RelativePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public double Duration { get; set; }

        public DateTimeOffset FileOpenTime { get; set; }

        public DateTimeOffset FileCloseTime { get; set; }
    }
}
