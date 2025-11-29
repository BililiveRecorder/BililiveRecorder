using System;
using BililiveRecorder.Core.SimpleWebhook;
using Newtonsoft.Json;

namespace BililiveRecorder.Core.Event
{
    /// <summary>
    /// <see cref="EventType.FileClosed"/>
    /// </summary>
    public sealed class RecordFileClosedEventArgs : RecordEventArgsBase, IRecordSessionEventArgs
    {
        internal RecordFileClosedEventArgs(IRoom room) : base(room) { }

        public Guid SessionId { get; set; }

        [JsonIgnore]
        public string FullPath { get; set; } = string.Empty;

        public string RelativePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public double Duration { get; set; }

        public DateTimeOffset FileOpenTime { get; set; }

        public DateTimeOffset FileCloseTime { get; set; }
    }
}
