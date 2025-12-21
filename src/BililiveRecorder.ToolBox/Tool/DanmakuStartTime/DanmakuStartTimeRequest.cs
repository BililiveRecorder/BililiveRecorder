using System;

namespace BililiveRecorder.ToolBox.Tool.DanmakuStartTime
{
    public class DanmakuStartTimeRequest : ICommandRequest<DanmakuStartTimeResponse>
    {
        public string[] Inputs { get; set; } = Array.Empty<string>();
    }
}
