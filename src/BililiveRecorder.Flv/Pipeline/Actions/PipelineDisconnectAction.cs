namespace BililiveRecorder.Flv.Pipeline.Actions
{
    public class PipelineDisconnectAction : PipelineAction
    {
        public string Reason { get; set; } = string.Empty;

        public PipelineDisconnectAction(string reason)
        {
            this.Reason = reason;
        }

        public override PipelineAction Clone() => new PipelineDisconnectAction(reason: this.Reason);
    }
}
