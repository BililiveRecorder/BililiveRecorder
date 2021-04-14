using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using BililiveRecorder.ToolBox.Commands;
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

        private void RegisterCommand<IHandler, IRequest, IResponse>(string name, string? description, Action<Command> configure)
            where IHandler : ICommandHandler<IRequest, IResponse>
            where IRequest : ICommandRequest<IResponse>
        {
            var cmd = new Command(name, description)
            {
                new Option<bool>("--json", "print result as json string"),
                new Option<bool>("--json-indented", "print result as indented json string")
            };
            cmd.Handler = CommandHandler.Create((IRequest r, bool json, bool jsonIndented) => RunSubCommand<IHandler, IRequest, IResponse>(r, json, jsonIndented));
            configure(cmd);
            this.Add(cmd);
        }

        private static async Task<int> RunSubCommand<IHandler, IRequest, IResponse>(IRequest request, bool json, bool jsonIndented)
            where IHandler : ICommandHandler<IRequest, IResponse>
            where IRequest : ICommandRequest<IResponse>
        {
            var handler = Activator.CreateInstance<IHandler>();

            var response = await handler.Handle(request).ConfigureAwait(false);

            if (json || jsonIndented)
            {
                var json_str = JsonConvert.SerializeObject(response, jsonIndented ? Formatting.Indented : Formatting.None);
                Console.WriteLine(json_str);
            }
            else
            {
                handler.PrintResponse(response);
            }

            return 0;
        }
    }
}
