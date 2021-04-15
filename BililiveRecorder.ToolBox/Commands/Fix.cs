using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Writer;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BililiveRecorder.ToolBox.Commands
{
    public class FixRequest : ICommandRequest<FixResponse>
    {
        public string Input { get; set; } = string.Empty;

        public string OutputBase { get; set; } = string.Empty;
    }

    public class FixResponse
    {
        public string InputPath { get; set; } = string.Empty;

        public string[] OutputPaths { get; set; } = Array.Empty<string>();

        public bool NeedFix { get; set; }
        public bool Unrepairable { get; set; }

        public int OutputFileCount { get; set; }

        public int IssueTypeOther { get; set; }
        public int IssueTypeUnrepairable { get; set; }
        public int IssueTypeTimestampJump { get; set; }
        public int IssueTypeDecodingHeader { get; set; }
        public int IssueTypeRepeatingData { get; set; }
    }

    public class FixHandler : ICommandHandler<FixRequest, FixResponse>
    {
        private static readonly ILogger logger = Log.ForContext<FixHandler>();

        public Task<FixResponse> Handle(FixRequest request) => this.Handle(request, null);

        public async Task<FixResponse> Handle(FixRequest request, Func<double, Task>? progress)
        {
            var inputPath = Path.GetFullPath(request.Input);

            var outputPaths = new List<string>();
            var targetProvider = new AutoFixFlvWriterTargetProvider(request.OutputBase);
            targetProvider.BeforeFileOpen += (sender, path) => outputPaths.Add(path);

            var memoryStreamProvider = new DefaultMemoryStreamProvider();
            var tagWriter = new FlvTagFileWriter(targetProvider, memoryStreamProvider, logger);

            var comments = new List<ProcessingComment>();
            var context = new FlvProcessingContext();
            var session = new Dictionary<object, object?>();

            {
                using var inputStream = File.OpenRead(inputPath);

                using var grouping = new TagGroupReader(new FlvTagPipeReader(PipeReader.Create(inputStream), memoryStreamProvider, skipData: false, logger: logger));
                using var writer = new FlvProcessingContextWriter(tagWriter: tagWriter, allowMissingHeader: true);
                var pipeline = new ProcessingPipelineBuilder(new ServiceCollection().BuildServiceProvider()).AddDefault().AddRemoveFillerData().Build();

                var count = 0;
                while (true)
                {
                    var group = await grouping.ReadGroupAsync(default).ConfigureAwait(false);
                    if (group is null)
                        break;

                    context.Reset(group, session);
                    pipeline(context);

                    if (context.Comments.Count > 0)
                    {
                        comments.AddRange(context.Comments);
                        logger.Debug("修复逻辑输出 {@Comments}", context.Comments);
                    }

                    await writer.WriteAsync(context).ConfigureAwait(false);

                    foreach (var action in context.Actions)
                        if (action is PipelineDataAction dataAction)
                            foreach (var tag in dataAction.Tags)
                                tag.BinaryData?.Dispose();

                    if (count++ % 10 == 0)
                    {
                        progress?.Invoke((double)inputStream.Position / inputStream.Length);
                    }
                }
            }

            var countableComments = comments.Where(x => x.T != CommentType.Logging);

            var response = new FixResponse
            {
                InputPath = inputPath,
                OutputPaths = outputPaths.ToArray(),
                OutputFileCount = outputPaths.Count,

                NeedFix = outputPaths.Count != 1 || countableComments.Any(),
                Unrepairable = countableComments.Any(x => x.T == CommentType.Unrepairable),

                IssueTypeOther = countableComments.Count(x => x.T == CommentType.Other),
                IssueTypeUnrepairable = countableComments.Count(x => x.T == CommentType.Unrepairable),
                IssueTypeTimestampJump = countableComments.Count(x => x.T == CommentType.TimestampJump),
                IssueTypeDecodingHeader = countableComments.Count(x => x.T == CommentType.DecodingHeader),
                IssueTypeRepeatingData = countableComments.Count(x => x.T == CommentType.RepeatingData)
            };

            return response;
        }

        public void PrintResponse(FixResponse response)
        {
            Console.Write("Input: ");
            Console.WriteLine(response.InputPath);

            Console.WriteLine(response.NeedFix ? "File needs repair" : "File doesn't need repair");

            if (response.Unrepairable)
                Console.WriteLine("File contains error(s) that are unrepairable (yet), please send sample to the author of this program.");

            Console.WriteLine("{0} file(s) written", response.OutputFileCount);

            foreach (var path in response.OutputPaths)
            {
                Console.Write("  ");
                Console.WriteLine(path);
            }

            Console.WriteLine("Types of error:");
            Console.Write("Other: ");
            Console.WriteLine(response.IssueTypeOther);
            Console.Write("Unrepairable: ");
            Console.WriteLine(response.IssueTypeUnrepairable);
            Console.Write("TimestampJump: ");
            Console.WriteLine(response.IssueTypeTimestampJump);
            Console.Write("DecodingHeader: ");
            Console.WriteLine(response.IssueTypeDecodingHeader);
            Console.Write("RepeatingData: ");
            Console.WriteLine(response.IssueTypeRepeatingData);
        }

        private class AutoFixFlvWriterTargetProvider : IFlvWriterTargetProvider
        {
            private readonly string pathTemplate;
            private int fileIndex = 1;

            public event EventHandler<string>? BeforeFileOpen;

            public AutoFixFlvWriterTargetProvider(string pathTemplate)
            {
                this.pathTemplate = pathTemplate;
            }

            public Stream CreateAlternativeHeaderStream()
            {
                var path = Path.ChangeExtension(this.pathTemplate, "header.txt");
                return File.Open(path, FileMode.Append, FileAccess.Write, FileShare.None);
            }

            public (Stream stream, object state) CreateOutputStream()
            {
                var i = this.fileIndex++;
                var path = Path.ChangeExtension(this.pathTemplate, $"fix_p{i}.flv");
                var fileStream = File.Create(path);
                BeforeFileOpen?.Invoke(this, path);
                return (fileStream, null!);
            }
        }
    }
}
