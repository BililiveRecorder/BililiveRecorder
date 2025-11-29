using BililiveRecorder.Core.Event;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BililiveRecorder.Core.SimpleWebhook
{
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum EventType
    {
        Unknown,

        /// <summary>
        /// 录制开始 <see cref="RecordSessionStartedEventArgs"/>
        /// </summary>
        SessionStarted,

        /// <summary>
        /// 录制结束 <see cref="RecordSessionEndedEventArgs"/>
        /// </summary>
        SessionEnded,

        /// <summary>
        /// 新建了文件 <see cref="RecordFileOpeningEventArgs"/>
        /// </summary>
        FileOpening,

        /// <summary>
        /// 文件写入结束 <see cref="RecordFileClosedEventArgs"/>
        /// </summary>
        FileClosed,

        /// <summary>
        /// 直播开始 <see cref="StreamStartedEventArgs"/>
        /// </summary>
        StreamStarted,

        /// <summary>
        /// 直播结束 <see cref="StreamEndedEventArgs"/>
        /// </summary>
        StreamEnded,
    }
}
