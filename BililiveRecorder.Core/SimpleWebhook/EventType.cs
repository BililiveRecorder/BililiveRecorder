using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BililiveRecorder.Core.SimpleWebhook
{
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum EventType
    {
        Unknown,
        /// <summary>
        /// 录制开始
        /// </summary>
        SessionStarted,
        /// <summary>
        /// 录制结束
        /// </summary>
        SessionEnded,
        /// <summary>
        /// 新建了文件
        /// </summary>
        FileOpening,
        /// <summary>
        /// 文件写入结束
        /// </summary>
        FileClosed,
        /// <summary>
        /// 直播开始
        /// </summary>
        StreamStarted,
        /// <summary>
        /// 直播结束
        /// </summary>
        StreamEnded,
    }
}
