using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.ToolBox.Tool.Fix
{
    public class FixRequest : ICommandRequest<FixResponse>
    {
        public string Input { get; set; } = string.Empty;

        public string OutputBase { get; set; } = string.Empty;

        public ProcessingPipelineSettings? PipelineSettings { get; set; }
    }
}
