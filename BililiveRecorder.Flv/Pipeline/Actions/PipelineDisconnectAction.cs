namespace BililiveRecorder.Flv.Pipeline.Actions
{
    public class PipelineDisconnectAction : PipelineAction
    {
        public static readonly PipelineDisconnectAction Instance = new PipelineDisconnectAction();

        public override PipelineAction Clone() => Instance;
    }
}
