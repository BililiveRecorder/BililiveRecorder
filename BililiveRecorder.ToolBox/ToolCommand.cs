using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using BililiveRecorder.ToolBox.Tool.Analyze;
using BililiveRecorder.ToolBox.Tool.Export;
using BililiveRecorder.ToolBox.Tool.Fix;
using Newtonsoft.Json;

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
            where TResponse : class
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
            where TResponse : class
        {
            var handler = Activator.CreateInstance<THandler>();

            var response = await handler.Handle(request).ConfigureAwait(false);

            if (json || jsonIndented)
            {
                var json_str = JsonConvert.SerializeObject(response, jsonIndented ? Formatting.Indented : Formatting.None);
                Console.WriteLine(json_str);
            }
            else
            {
                if (response.Status == ResponseStatus.OK)
                {
                    handler.PrintResponse(response.Result!);
                }
                else
                {
                    Console.Write("Error: ");
                    Console.WriteLine(response.Status);
                    Console.WriteLine(response.ErrorMessage);
                }
            }

            return 0;
        }
    }
}
