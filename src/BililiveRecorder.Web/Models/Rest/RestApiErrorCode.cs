namespace BililiveRecorder.Web.Models.Rest
{
    public enum RestApiErrorCode : int
    {
        /// <summary>
        /// 错误
        /// </summary>
        Unknown = -1,
        /// <summary>
        /// 房间号不在允许的范围内
        /// </summary>
        RoomidOutOfRange = -2,
        /// <summary>
        /// 房间已存在
        /// </summary>
        RoomExist = -3,
        /// <summary>
        /// 房间不存在
        /// </summary>
        RoomNotFound = -4,
    }
}
