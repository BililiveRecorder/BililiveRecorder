namespace BililiveRecorder.Flv.Pipeline
{
    public class PipelineDisconnectAction : PipelineAction
    {
        public static readonly PipelineDisconnectAction Instance = new PipelineDisconnectAction();

        public override PipelineAction Clone() => Instance;
    }
}
