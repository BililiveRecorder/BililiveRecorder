namespace BililiveRecorder.Flv.Pipeline
{
    public class PipelineEndAction : PipelineAction
    {
        private Tag Tag { get; set; }

        public PipelineEndAction(Tag tag)
        {
            this.Tag = tag ?? throw new System.ArgumentNullException(nameof(tag));
        }

        public override PipelineAction Clone() => new PipelineEndAction(this.Tag.Clone());
    }
}
