namespace BililiveRecorder.ToolBox.Tool.Export
{
    public class ExportRequest : ICommandRequest<ExportResponse>
    {
        public string Input { get; set; } = string.Empty;

        public string Output { get; set; } = string.Empty;
    }
}
