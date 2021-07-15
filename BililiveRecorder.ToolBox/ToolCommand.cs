using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using BililiveRecorder.ToolBox.Tool.Analyze;
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
            });

            this.RegisterCommand<FixHandler, FixRequest, FixResponse>("fix", null, c =>
            {
                c.Add(new Argument<string>("input", "example: input.flv"));
                c.Add(new Argument<string>("output-base", "example: output.flv"));
            });

            this.RegisterCommand<ExportHandler, ExportRequest, ExportResponse>("export", null, c =>
            {
                c.Add(new Argument<string>("input", "example: input.flv"));
                c.Add(new Argument<string>("output", "example: output.brec.xml.gz"));
            });
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
                        var r = await handler.Handle(request, default, async p => t.Value = p).ConfigureAwait(false);
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
                    AnsiConsole.Render(new FigletText("Error").Color(Color.Red));

                    var errorInfo = new Table
                    {
                        Border = TableBorder.Rounded
                    };
                    errorInfo.AddColumn(new TableColumn("Error Code").Centered());
                    errorInfo.AddColumn(new TableColumn("Error Message").Centered());
                    errorInfo.AddRow("[red]" + response.Status.ToString().EscapeMarkup() + "[/]", "[red]" + (response.ErrorMessage ?? string.Empty) + "[/]");
                    AnsiConsole.Render(errorInfo);

                    if (response.Exception is not null)
                        AnsiConsole.Render(new Panel(response.Exception.GetRenderable(ExceptionFormats.ShortenPaths | ExceptionFormats.ShowLinks))
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
