using System;

namespace BililiveRecorder.ToolBox.Tool.DanmakuMerger
{
    public class DanmakuMergerRequest : ICommandRequest<DanmakuMergerResponse>
    {
        public string[] Inputs { get; set; } = Array.Empty<string>();

        public string Output { get; set; } = string.Empty;
    }
}
