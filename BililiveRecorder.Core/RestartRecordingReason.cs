namespace BililiveRecorder.Core
{
    public enum RestartRecordingReason
    {
        /// <summary>
        /// 普通重试
        /// </summary>
        GenericRetry,

        /// <summary>
        /// 无对应直播画质
        /// </summary>
        NoMatchingQnValue,
    }
}
