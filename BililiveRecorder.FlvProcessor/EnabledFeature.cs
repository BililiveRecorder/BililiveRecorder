namespace BililiveRecorder.FlvProcessor
{
    public enum EnabledFeature : int
    {
        /// <summary>
        /// 同时启用两个功能
        /// </summary>
        Both,

        /// <summary>
        /// 只使用即时回放剪辑功能
        /// </summary>
        ClipOnly,

        /// <summary>
        /// 只使用录制功能
        /// </summary>
        RecordOnly,
    }

    public static class EnabledFeatureExtenstion
    {
        public static bool IsClipEnabled(this EnabledFeature enabledFeature)
        {
            return enabledFeature == EnabledFeature.Both || enabledFeature == EnabledFeature.ClipOnly;
        }
        public static bool IsRecordEnabled(this EnabledFeature enabledFeature)
        {
            return enabledFeature == EnabledFeature.Both || enabledFeature == EnabledFeature.RecordOnly;
        }
    }
}
