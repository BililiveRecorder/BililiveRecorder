using System;
using Newtonsoft.Json;

namespace BililiveRecorder.Core.Event
{
    public class RecordFileOpeningEventArgs : RecordEventArgsBase
    {
        public RecordFileOpeningEventArgs() { }

        public RecordFileOpeningEventArgs(IRoom room) : base(room) { }

        [JsonIgnore]
        public string FullPath { get; set; } = string.Empty;

        public string RelativePath { get; set; } = string.Empty;

        public DateTimeOffset FileOpenTime { get; set; }
    }
}
