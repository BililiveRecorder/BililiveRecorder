namespace BililiveRecorder.ToolBox.Tool.Analyze
{
    public class AnalyzeRequest : ICommandRequest<AnalyzeResponse>
    {
        public string Input { get; set; } = string.Empty;
    }
}
