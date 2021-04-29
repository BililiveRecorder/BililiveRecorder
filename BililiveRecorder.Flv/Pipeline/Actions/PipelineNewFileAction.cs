namespace BililiveRecorder.Flv.Pipeline.Actions
{
    public class PipelineNewFileAction : PipelineAction
    {
        public static readonly PipelineNewFileAction Instance = new PipelineNewFileAction();

        public override PipelineAction Clone() => Instance;
    }
}
