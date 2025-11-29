using System;
using Spectre.Console;

namespace BililiveRecorder.ToolBox.Tool.DanmakuStartTime
{
    public class DanmakuStartTimeResponse : IResponseData
    {
        public DanmakuStartTime[] StartTimes { get; set; } = Array.Empty<DanmakuStartTime>();

        public void PrintToConsole()
        {
            var t = new Table()
                .AddColumns("Start Time", "File Path")
                .Border(TableBorder.Rounded);

            foreach (var item in this.StartTimes)
            {
                t.AddRow(item.StartTime.ToString().EscapeMarkup(), item.Path.EscapeMarkup());
            }

            AnsiConsole.Write(t);
        }

        public class DanmakuStartTime
        {
            public string Path { get; set; } = string.Empty;
            public DateTimeOffset StartTime { get; set; }
        }
    }
}
