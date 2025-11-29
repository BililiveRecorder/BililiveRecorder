using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Pipeline.Actions;
using BililiveRecorder.Flv.Pipeline.Rules;
using BililiveRecorder.Flv.Writer;
using BililiveRecorder.Flv.Xml;
using BililiveRecorder.ToolBox.ProcessingRules;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BililiveRecorder.ToolBox.Tool.Analyze
{
    public class AnalyzeHandler : ICommandHandler<AnalyzeRequest, AnalyzeResponse>
    {
        private static readonly ILogger logger = Log.ForContext<AnalyzeHandler>();

        public string Name => "Analyze";

        public async Task<CommandResponse<AnalyzeResponse>> Handle(AnalyzeRequest request, CancellationToken cancellationToken, ProgressCallback? progress)
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
                    else if (inputPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        tagReader = await Task.Run(() =>
                        {
                            using var zip = new ZipArchive(File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read), ZipArchiveMode.Read, false, Encoding.UTF8);
                            var entry = zip.Entries.First(x => x.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
                            var xmlFlvFile = (XmlFlvFile)XmlFlvFile.Serializer.Deserialize(entry.Open());
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
                using var writer = new FlvProcessingContextWriter(tagWriter: tagWriter, allowMissingHeader: true, disableKeyframes: true, logger: logger);
                var statsRule = new StatsRule();
                var ffmpegDetectionRule = new FfmpegDetectionRule();
                var pipeline = new ProcessingPipelineBuilder()
                    .ConfigureServices(services => services.AddSingleton(request.PipelineSettings ?? new ProcessingPipelineSettings()))
                    .AddRule(statsRule)
                    .AddRule(ffmpegDetectionRule)
                    .AddDefaultRules()
                    .AddRemoveFillerDataRule()
                    .Build();

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

                    var countableComments = comments.Where(x => x.ActionRequired).ToArray();
                    return new AnalyzeResponse
                    {
                        InputPath = inputPath,

                        NeedFix = tagWriter.OutputFileCount != 1 || countableComments.Any(),
                        Unrepairable = countableComments.Any(x => x.Type == CommentType.Unrepairable),
                        FfmpegDetected = ffmpegDetectionRule.LavfEncoderDetected && ffmpegDetectionRule.EndTagDetected,

                        OutputFileCount = tagWriter.OutputFileCount,

                        VideoStats = videoStats,
                        AudioStats = audioStats,

                        IssueTypeOther = countableComments.Count(x => x.Type == CommentType.Other),
                        IssueTypeUnrepairable = countableComments.Count(x => x.Type == CommentType.Unrepairable),
                        IssueTypeTimestampJump = countableComments.Count(x => x.Type == CommentType.TimestampJump),
                        IssueTypeTimestampOffset = countableComments.Count(x => x.Type == CommentType.TimestampOffset),
                        IssueTypeDecodingHeader = countableComments.Count(x => x.Type == CommentType.DecodingHeader),
                        IssueTypeRepeatingData = countableComments.Count(x => x.Type == CommentType.RepeatingData)
                    };
                });

                return new CommandResponse<AnalyzeResponse>
                {
                    Status = ResponseStatus.OK,
                    Data = response
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
            public Task WriteAccompanyingTextLog(double lastTagDuration, string message) => Task.CompletedTask;
            public Task WriteTag(Tag tag) => Task.CompletedTask;
        }
    }
}
