using BililiveRecorder.ToolBox.ProcessingRules;

namespace BililiveRecorder.ToolBox.Tool.Analyze
{
    public class AnalyzeResponse
    {
        public string InputPath { get; set; } = string.Empty;

        public bool NeedFix { get; set; }
        public bool Unrepairable { get; set; }

        public int OutputFileCount { get; set; }

        public FlvStats? VideoStats { get; set; }
        public FlvStats? AudioStats { get; set; }

        public int IssueTypeOther { get; set; }
        public int IssueTypeUnrepairable { get; set; }
        public int IssueTypeTimestampJump { get; set; }
        public int IssueTypeTimestampOffset { get; set; }
        public int IssueTypeDecodingHeader { get; set; }
        public int IssueTypeRepeatingData { get; set; }
    }
}
