using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Pipeline.Actions;
using BililiveRecorder.Flv.Writer;
using BililiveRecorder.Flv.Xml;
using BililiveRecorder.ToolBox.ProcessingRules;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BililiveRecorder.ToolBox.Commands
{
    public class AnalyzeRequest : ICommandRequest<AnalyzeResponse>
    {
        public string Input { get; set; } = string.Empty;
    }

    public class AnalyzeResponse
    {
        public string InputPath { get; set; } = string.Empty;

        public bool NeedFix { get; set; }
        public bool Unrepairable { get; set; }

        public int OutputFileCount { get; set; }

        public FlvStats? VideoStats { get; set; }
        public FlvStats? AudioStats { get; set; }

        public int IssueTypeOther { get; set; }
        public int IssueTypeUnrepairable { get; set; }
        public int IssueTypeTimestampJump { get; set; }
        public int IssueTypeTimestampOffset { get; set; }
        public int IssueTypeDecodingHeader { get; set; }
        public int IssueTypeRepeatingData { get; set; }
    }

    public class AnalyzeHandler : ICommandHandler<AnalyzeRequest, AnalyzeResponse>
    {
        private static readonly ILogger logger = Log.ForContext<AnalyzeHandler>();

        public Task<CommandResponse<AnalyzeResponse>> Handle(AnalyzeRequest request) => this.Handle(request, default, null);

        public async Task<CommandResponse<AnalyzeResponse>> Handle(AnalyzeRequest request, CancellationToken cancellationToken, Func<double, Task>? progress)
        {
            FileStream? flvFileStream = null;
            try
            {
                XmlFlvFile.XmlFlvFileMeta? meta = null;

                var memoryStreamProvider = new RecyclableMemoryStreamProvider();
                var comments = new List<ProcessingComment>();
                var context = new FlvProcessingContext();
                var session = new Dictionary<object, object?>();

                // Input
                string? inputPath;
                IFlvTagReader tagReader;
                try
                {
                    inputPath = Path.GetFullPath(request.Input);
                    if (inputPath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                        tagReader = await Task.Run(() =>
                        {
                            using var stream = new GZipStream(File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);
                            var xmlFlvFile = (XmlFlvFile)XmlFlvFile.Serializer.Deserialize(stream);
                            meta = xmlFlvFile.Meta;
                            return new FlvTagListReader(xmlFlvFile.Tags);
                        });
                    else if (inputPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        tagReader = await Task.Run(() =>
                        {
                            using var stream = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            var xmlFlvFile = (XmlFlvFile)XmlFlvFile.Serializer.Deserialize(stream);
                            meta = xmlFlvFile.Meta;
                            return new FlvTagListReader(xmlFlvFile.Tags);
                        });
                    else
                    {
                        flvFileStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                        tagReader = new FlvTagPipeReader(PipeReader.Create(flvFileStream), memoryStreamProvider, skipData: false, logger: logger);
                    }
                }
                catch (Exception ex) when (ex is not FlvException)
                {
                    return new CommandResponse<AnalyzeResponse>
                    {
                        Status = ResponseStatus.InputIOError,
                        Exception = ex,
                        ErrorMessage = ex.Message
                    };
                }

                // Output
                var tagWriter = new AnalyzeMockFlvTagWriter();

                // Pipeline
                using var grouping = new TagGroupReader(tagReader);
                using var writer = new FlvProcessingContextWriter(tagWriter: tagWriter, allowMissingHeader: true, disableKeyframes: true);
                var statsRule = new StatsRule();
                var pipeline = new ProcessingPipelineBuilder(new ServiceCollection().BuildServiceProvider()).Add(statsRule).AddDefault().AddRemoveFillerData().Build();

                // Run
                await Task.Run(async () =>
                {
                    var count = 0;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var group = await grouping.ReadGroupAsync(cancellationToken).ConfigureAwait(false);
                        if (group is null)
                            break;

                        context.Reset(group, session);
                        pipeline(context);

                        if (context.Comments.Count > 0)
                        {
                            comments.AddRange(context.Comments);
                            logger.Debug("分析逻辑输出 {@Comments}", context.Comments);
                        }

                        await writer.WriteAsync(context).ConfigureAwait(false);

                        foreach (var action in context.Actions)
                            if (action is PipelineDataAction dataAction)
                                foreach (var tag in dataAction.Tags)
                                    tag.BinaryData?.Dispose();

                        if (count++ % 10 == 0 && flvFileStream is not null && progress is not null)
                            await progress((double)flvFileStream.Position / flvFileStream.Length);
                    }
                }).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    return new CommandResponse<AnalyzeResponse> { Status = ResponseStatus.Cancelled };

                if (meta is not null)
                    logger.Information("Xml meta: {@Meta}", meta);

                // Result
                var response = await Task.Run(() =>
                {
                    var (videoStats, audioStats) = statsRule.GetStats();

                    var countableComments = comments.Where(x => x.T != CommentType.Logging).ToArray();
                    return new AnalyzeResponse
                    {
                        InputPath = inputPath,

                        NeedFix = tagWriter.OutputFileCount != 1 || countableComments.Any(),
                        Unrepairable = countableComments.Any(x => x.T == CommentType.Unrepairable),

                        OutputFileCount = tagWriter.OutputFileCount,

                        VideoStats = videoStats,
                        AudioStats = audioStats,

                        IssueTypeOther = countableComments.Count(x => x.T == CommentType.Other),
                        IssueTypeUnrepairable = countableComments.Count(x => x.T == CommentType.Unrepairable),
                        IssueTypeTimestampJump = countableComments.Count(x => x.T == CommentType.TimestampJump),
                        IssueTypeTimestampOffset = countableComments.Count(x => x.T == CommentType.TimestampOffset),
                        IssueTypeDecodingHeader = countableComments.Count(x => x.T == CommentType.DecodingHeader),
                        IssueTypeRepeatingData = countableComments.Count(x => x.T == CommentType.RepeatingData)
                    };
                });

                return new CommandResponse<AnalyzeResponse>
                {
                    Status = ResponseStatus.OK,
                    Result = response
                };
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new CommandResponse<AnalyzeResponse> { Status = ResponseStatus.Cancelled };
            }
            catch (NotFlvFileException ex)
            {
                return new CommandResponse<AnalyzeResponse>
                {
                    Status = ResponseStatus.NotFlvFile,
                    Exception = ex,
                    ErrorMessage = ex.Message
                };
            }
            catch (UnknownFlvTagTypeException ex)
            {
                return new CommandResponse<AnalyzeResponse>
                {
                    Status = ResponseStatus.UnknownFlvTagType,
                    Exception = ex,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new CommandResponse<AnalyzeResponse>
                {
                    Status = ResponseStatus.Error,
                    Exception = ex,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                flvFileStream?.Dispose();
            }
        }

        public void PrintResponse(AnalyzeResponse response)
        {
            Console.Write("Input: ");
            Console.WriteLine(response.InputPath);

            Console.WriteLine(response.NeedFix ? "File needs repair" : "File doesn't need repair");

            if (response.Unrepairable)
                Console.WriteLine("File contains error(s) that are unrepairable (yet), please send sample to the author of this program.");

            Console.WriteLine("Will output {0} file(s) if repaired", response.OutputFileCount);

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

        private class AnalyzeMockFlvTagWriter : IFlvTagWriter
        {
            public long FileSize => 0;
            public object? State => null;

            public int OutputFileCount { get; private set; }

            public bool CloseCurrentFile() => true;
            public Task CreateNewFile()
            {
                this.OutputFileCount++;
                return Task.CompletedTask;
            }

            public void Dispose() { }
            public Task OverwriteMetadata(ScriptTagBody metadata) => Task.CompletedTask;
            public Task WriteAlternativeHeaders(IEnumerable<Tag> tags) => Task.CompletedTask;
            public Task WriteTag(Tag tag) => Task.CompletedTask;
        }
    }
}
