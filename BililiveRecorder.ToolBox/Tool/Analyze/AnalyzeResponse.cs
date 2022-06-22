using BililiveRecorder.ToolBox.ProcessingRules;
using Spectre.Console;

namespace BililiveRecorder.ToolBox.Tool.Analyze
{
    public class AnalyzeResponse : IResponseData
    {
        public string InputPath { get; set; } = string.Empty;

        public bool NeedFix { get; set; }
        public bool Unrepairable { get; set; }
        public bool FfmpegDetected { get; set; }

        public int OutputFileCount { get; set; }

        public FlvStats? VideoStats { get; set; }
        public FlvStats? AudioStats { get; set; }

        public int IssueTypeOther { get; set; }
        public int IssueTypeUnrepairable { get; set; }
        public int IssueTypeTimestampJump { get; set; }
        public int IssueTypeTimestampOffset { get; set; }
        public int IssueTypeDecodingHeader { get; set; }
        public int IssueTypeRepeatingData { get; set; }

        public void PrintToConsole()
        {
            if (this.NeedFix)
                AnsiConsole.Write(new FigletText("Need Fix").Color(Color.Red));
            else
                AnsiConsole.Write(new FigletText("All Good").Color(Color.Green));

            if (this.Unrepairable)
            {
                AnsiConsole.Write(new Panel("This file contains error(s) that are identified as unrepairable (yet).\n" +
                    "Please check if you're using the newest version.\n" +
                    "Please consider send a sample file to the developer.")
                {
                    Header = new PanelHeader("Important Note"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(foreground: Color.Red)
                });
            }

            if (this.FfmpegDetected)
            {
                AnsiConsole.Write(new Panel("This file seems like it was written by FFmpeg.\n" +
                    "It might no longer possible to fix this file, if there's any problem.\n" +
                    "Only unprocessed data taken directly from the stream server could be fixed.")
                {
                    Header = new PanelHeader("[bold yellow]FFmpeg Detected[/]"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(foreground: Color.Yellow)
                });
            }

            AnsiConsole.Write(new Panel(this.InputPath.EscapeMarkup())
            {
                Header = new PanelHeader("Input"),
                Border = BoxBorder.Rounded
            });

            AnsiConsole.MarkupLine("Will output [lime]{0}[/] file(s) if repaired", this.OutputFileCount);

            AnsiConsole.Write(new Table()
                .Border(TableBorder.Rounded)
                .AddColumns("Category", "Count")
                .AddRow("Unrepairable", this.IssueTypeUnrepairable.ToString())
                .AddRow("Other", this.IssueTypeOther.ToString())
                .AddRow("TimestampJump", this.IssueTypeTimestampJump.ToString())
                .AddRow("TimestampOffset", this.IssueTypeTimestampOffset.ToString())
                .AddRow("DecodingHeader", this.IssueTypeDecodingHeader.ToString())
                .AddRow("RepeatingData", this.IssueTypeRepeatingData.ToString())
                );
        }
    }
}
