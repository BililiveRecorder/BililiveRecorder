using System;
using BililiveRecorder.Core.SimpleWebhook;

namespace BililiveRecorder.Core.Event
{
    /// <summary>
    /// <see cref="EventType.SessionStarted"/>
    /// </summary>
    public sealed class RecordSessionStartedEventArgs : RecordEventArgsBase, IRecordSessionEventArgs
    {
        internal RecordSessionStartedEventArgs(IRoom room) : base(room) { }

        public Guid SessionId { get; set; }

        /// <summary>
        /// 录制画质 qn 值
        /// </summary>
        public int Qn { get; set; }

        /// <summary>
        /// 录制画质描述
        /// </summary>
        public string QnDescription { get; set; } = string.Empty;
    }
}
