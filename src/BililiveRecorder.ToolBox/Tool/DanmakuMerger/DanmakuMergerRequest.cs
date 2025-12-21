using System;

namespace BililiveRecorder.ToolBox.Tool.DanmakuMerger
{
    public class DanmakuMergerRequest : ICommandRequest<DanmakuMergerResponse>
    {
        public string[] Inputs { get; set; } = Array.Empty<string>();

        public int[]? Offsets { get; set; } = null;

        public string Output { get; set; } = string.Empty;
    }
}
