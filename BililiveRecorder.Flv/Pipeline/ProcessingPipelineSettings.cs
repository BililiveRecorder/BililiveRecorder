namespace BililiveRecorder.Flv.Pipeline
{
    public class ProcessingPipelineSettings
    {
        public ProcessingPipelineSettings()
        { }

        /// <summary>
        /// 控制收到 onMetaData 时是否分段
        /// </summary>
        public bool SplitOnScriptTag { get; set; } = false;

        /// <summary>
        /// 检测到 H264 Annex-B 时禁用修复分段
        /// </summary>
        public bool DisableSplitOnH264AnnexB { get; set; } = false;
    }
}
