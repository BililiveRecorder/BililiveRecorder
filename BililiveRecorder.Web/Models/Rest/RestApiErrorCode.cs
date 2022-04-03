using System.Text.Json.Serialization;

namespace BililiveRecorder.Web.Models.Rest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RestApiErrorCode
    {
        /// <summary>
        /// 错误
        /// </summary>
        Unknown,
        /// <summary>
        /// 房间号不在允许的范围内
        /// </summary>
        RoomidOutOfRange,
        /// <summary>
        /// 房间已存在
        /// </summary>
        RoomExist,
        /// <summary>
        /// 房间不存在
        /// </summary>
        RoomNotFound,
    }
}
