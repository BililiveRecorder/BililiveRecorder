using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.ToolBox.Tool.Analyze;
using BililiveRecorder.ToolBox.Tool.DanmakuMerger;
using BililiveRecorder.ToolBox.Tool.DanmakuStartTime;
using BililiveRecorder.ToolBox.Tool.Export;
using BililiveRecorder.ToolBox.Tool.Fix;
using Newtonsoft.Json;
using Spectre.Console;

namespace BililiveRecorder.ToolBox
{
    public class ToolCommand : Command
    {
        public ToolCommand() : base("tool", "Run Tools")
        {
            this.RegisterCommand<AnalyzeHandler, AnalyzeRequest, AnalyzeResponse>("analyze", null, c =>
            {
                c.Add(new Argument<string>("input", "example: input.flv"));
                c.Add(new Option<ProcessingPipelineSettings?>(name: "--pipeline-settings", parseArgument: this.ParseProcessingPipelineSettings));
            });

            this.RegisterCommand<FixHandler, FixRequest, FixResponse>("fix", null, c =>
            {
                c.Add(new Argument<string>("input", "example: input.flv"));
                c.Add(new Argument<string>("output-base", "example: output.flv"));
                c.Add(new Option<ProcessingPipelineSettings?>(name: "--pipeline-settings", parseArgument: this.ParseProcessingPipelineSettings));
            });

            this.RegisterCommand<ExportHandler, ExportRequest, ExportResponse>("export", null, c =>
            {
                c.Add(new Argument<string>("input", "example: input.flv"));
                c.Add(new Argument<string>("output", "example: output.xml or output.zip"));
            });

            this.RegisterCommand<DanmakuStartTimeHandler, DanmakuStartTimeRequest, DanmakuStartTimeResponse>("danmaku-start-time", null, c =>
            {
                c.Add(new Argument<string[]>("inputs", "example: 1.xml 2.xml ..."));
            });

            this.RegisterCommand<DanmakuMergerHandler, DanmakuMergerRequest, DanmakuMergerResponse>("danmaku-merge", null, c =>
            {
                c.Add(new Argument<string>("output", "example: output.xml"));
                c.Add(new Argument<string[]>("inputs", "example: 1.xml 2.xml ..."));
                c.Add(new Option<int[]?>("--offsets", "Use offsets provided instead of calculating from starttime attribute."));
            });
        }

        private ProcessingPipelineSettings? ParseProcessingPipelineSettings(ArgumentResult result)
        {
            if (result.Tokens.Count == 0) return null;

            try
            {
                return JsonConvert.DeserializeObject<ProcessingPipelineSettings>(result.Tokens.Single().Value);
            }
            catch (Exception)
            {
                result.ErrorMessage = "Pipeline settings must be a valid json string";
                return null;
            }
        }

        private void RegisterCommand<THandler, TRequest, TResponse>(string name, string? description, Action<Command> configure)
            where THandler : ICommandHandler<TRequest, TResponse>
            where TRequest : ICommandRequest<TResponse>
            where TResponse : IResponseData
        {
            var cmd = new Command(name, description)
            {
                new Option<bool>("--json", "print result as json string"),
                new Option<bool>("--json-indented", "print result as indented json string")
            };
            cmd.Handler = CommandHandler.Create((TRequest r, bool json, bool jsonIndented) => RunSubCommand<THandler, TRequest, TResponse>(r, json, jsonIndented));
            configure(cmd);
            this.Add(cmd);
        }

        private static async Task<int> RunSubCommand<THandler, TRequest, TResponse>(TRequest request, bool json, bool jsonIndented)
            where THandler : ICommandHandler<TRequest, TResponse>
            where TRequest : ICommandRequest<TResponse>
            where TResponse : IResponseData
        {
            var isInteractive = !(json || jsonIndented);
            var handler = Activator.CreateInstance<THandler>();


            CommandResponse<TResponse>? response;
            if (isInteractive)
            {
                response = await AnsiConsole
                    .Progress()
                    .Columns(new ProgressColumn[]
                    {
                        new TaskDescriptionColumn(),
                        new ProgressBarColumn(),
                        new PercentageColumn(),
                        new SpinnerColumn(Spinner.Known.Dots10),
                    })
                    .StartAsync(async ctx =>
                    {
                        var t = ctx.AddTask(handler.Name);
                        t.MaxValue = 1d;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                        var r = await handler.Handle(request, default, async p => t.Value = p).ConfigureAwait(false);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                        t.Value = 1d;
                        return r;
                    })
                    .ConfigureAwait(false);
            }
            else
            {
                response = await handler.Handle(request, default, null).ConfigureAwait(false);
            }

            if (isInteractive)
            {
                if (response.Status == ResponseStatus.OK)
                {
                    response.Data?.PrintToConsole();

                    return 0;
                }
                else
                {
                    AnsiConsole.Write(new FigletText("Error").Color(Color.Red));

                    var errorInfo = new Table
                    {
                        Border = TableBorder.Rounded
                    };
                    errorInfo.AddColumn(new TableColumn("Error Code").Centered());
                    errorInfo.AddColumn(new TableColumn("Error Message").Centered());
                    errorInfo.AddRow("[red]" + response.Status.ToString().EscapeMarkup() + "[/]", "[red]" + (response.ErrorMessage ?? string.Empty) + "[/]");
                    AnsiConsole.Write(errorInfo);

                    if (response.Exception is not null)
                        AnsiConsole.Write(new Panel(response.Exception.GetRenderable(ExceptionFormats.ShortenPaths | ExceptionFormats.ShowLinks))
                        {
                            Header = new PanelHeader("Exception Info"),
                            Border = BoxBorder.Rounded
                        });

                    return 1;
                }
            }
            else
            {
                var json_str = JsonConvert.SerializeObject(response, jsonIndented ? Formatting.Indented : Formatting.None);
                Console.WriteLine(json_str);

                return response.Status == ResponseStatus.OK ? 0 : 1;
            }
        }
    }
}
