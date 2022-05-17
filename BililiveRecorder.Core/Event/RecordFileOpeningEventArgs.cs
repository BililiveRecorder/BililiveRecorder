using System;
using BililiveRecorder.Core.SimpleWebhook;
using Newtonsoft.Json;

namespace BililiveRecorder.Core.Event
{
    /// <summary>
    /// <see cref="EventType.FileOpening"/>
    /// </summary>
    public sealed class RecordFileOpeningEventArgs : RecordEventArgsBase, IRecordSessionEventArgs
    {
        internal RecordFileOpeningEventArgs(IRoom room) : base(room) { }

        public Guid SessionId { get; set; }

        [JsonIgnore]
        public string FullPath { get; set; } = string.Empty;

        public string RelativePath { get; set; } = string.Empty;

        public DateTimeOffset FileOpenTime { get; set; }
    }
}
