namespace BililiveRecorder.Flv.Pipeline.Actions
{
    public abstract class PipelineAction
    {
        protected PipelineAction()
        {
            this.Name = this.GetType().Name;
        }

        public string Name { get; }
        public abstract PipelineAction Clone();
    }
}
