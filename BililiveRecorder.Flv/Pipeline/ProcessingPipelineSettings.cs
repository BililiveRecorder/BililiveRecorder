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
    }
}
