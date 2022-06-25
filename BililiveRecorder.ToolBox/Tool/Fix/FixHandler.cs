using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using BililiveRecorder.Flv;
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

namespace BililiveRecorder.ToolBox.Tool.Fix
{
    public class FixHandler : ICommandHandler<FixRequest, FixResponse>
    {
        private static readonly ILogger logger = Log.ForContext<FixHandler>();

        public string Name => "Fix";

        public async Task<CommandResponse<FixResponse>> Handle(FixRequest request, CancellationToken cancellationToken, ProgressCallback? progress)
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
                var xmlMode = false;
                try
                {
                    inputPath = Path.GetFullPath(request.Input);
                    if (inputPath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                    {
                        xmlMode = true;
                        tagReader = await Task.Run(() =>
                        {
                            using var stream = new GZipStream(File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);
                            var xmlFlvFile = (XmlFlvFile)XmlFlvFile.Serializer.Deserialize(stream);
                            meta = xmlFlvFile.Meta;
                            return new FlvTagListReader(xmlFlvFile.Tags);
                        });
                    }
                    else if (inputPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        xmlMode = true;
                        tagReader = await Task.Run(() =>
                        {
                            using var stream = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            var xmlFlvFile = (XmlFlvFile)XmlFlvFile.Serializer.Deserialize(stream);
                            meta = xmlFlvFile.Meta;
                            return new FlvTagListReader(xmlFlvFile.Tags);
                        });
                    }
                    else if (inputPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        xmlMode = true;
                        tagReader = await Task.Run(() =>
                        {
                            using var zip = new ZipArchive(File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read), ZipArchiveMode.Read, false, Encoding.UTF8);
                            var entry = zip.Entries.First(x => x.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
                            var xmlFlvFile = (XmlFlvFile)XmlFlvFile.Serializer.Deserialize(entry.Open());
                            meta = xmlFlvFile.Meta;
                            return new FlvTagListReader(xmlFlvFile.Tags);
                        });
                    }
                    else
                    {
                        flvFileStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                        tagReader = new FlvTagPipeReader(PipeReader.Create(flvFileStream), memoryStreamProvider, skipData: false, logger: logger);
                    }
                }
                catch (Exception ex) when (ex is not FlvException)
                {
                    return new CommandResponse<FixResponse>
                    {
                        Status = ResponseStatus.InputIOError,
                        Exception = ex,
                        ErrorMessage = ex.Message
                    };
                }

                // Output
                var outputPaths = new List<string>();
                IFlvTagWriter tagWriter;
                if (xmlMode)
                    tagWriter = new FlvTagListWriter();
                else
                {
                    var targetProvider = new AutoFixFlvWriterTargetProvider(request.OutputBase);
                    targetProvider.BeforeFileOpen += (sender, path) => outputPaths.Add(path);
                    tagWriter = new FlvTagFileWriter(targetProvider, memoryStreamProvider, logger);
                }

                // Pipeline
                using var grouping = new TagGroupReader(tagReader);
                using var writer = new FlvProcessingContextWriter(tagWriter: tagWriter, allowMissingHeader: true, disableKeyframes: false, logger: logger);
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
                            logger.Debug("修复逻辑输出 {@Comments}", context.Comments);
                        }

                        await writer.WriteAsync(context).ConfigureAwait(false);

                        foreach (var action in context.Actions)
                            if (action is PipelineDataAction dataAction)
                                foreach (var tag in dataAction.Tags)
                                    tag.BinaryData?.Dispose();

                        if (count++ % 10 == 0 && progress is not null && flvFileStream is not null)
                            await progress((double)flvFileStream.Position / flvFileStream.Length);
                    }
                }).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    return new CommandResponse<FixResponse> { Status = ResponseStatus.Cancelled };

                // Post Run
                if (meta is not null)
                    logger.Information("Xml meta: {@Meta}", meta);

                if (xmlMode)
                    await Task.Run(() =>
                    {
                        var w = (FlvTagListWriter)tagWriter;

                        for (var i = 0; i < w.Files.Count; i++)
                        {
                            var path = Path.ChangeExtension(request.OutputBase, $"fix_p{i + 1:D3}.brec.xml");
                            outputPaths.Add(path);

                            using var file = XmlWriter.Create(File.Create(path), new()
                            {
                                Encoding = Encoding.UTF8,
                                Indent = true
                            });
                            XmlFlvFile.Serializer.Serialize(file, new XmlFlvFile { Tags = w.Files[i] });
                        }

                        if (w.AccompanyingTextLogs.Count > 0)
                        {
                            var path = Path.ChangeExtension(request.OutputBase, "txt");
                            using var writer = new StreamWriter(File.Open(path, FileMode.Append, FileAccess.Write, FileShare.None));
                            foreach (var (lastTagDuration, message) in w.AccompanyingTextLogs)
                            {
                                writer.WriteLine();
                                writer.WriteLine(lastTagDuration);
                                writer.WriteLine(message);
                            }
                        }
                    });

                if (cancellationToken.IsCancellationRequested)
                    return new CommandResponse<FixResponse> { Status = ResponseStatus.Cancelled };

                // Result
                var response = await Task.Run(() =>
                {
                    var (videoStats, audioStats) = statsRule.GetStats();

                    var countableComments = comments.Where(x => x.ActionRequired).ToArray();
                    return new FixResponse
                    {
                        InputPath = inputPath,
                        OutputPaths = outputPaths.ToArray(),
                        OutputFileCount = outputPaths.Count,

                        NeedFix = outputPaths.Count != 1 || countableComments.Any(),
                        Unrepairable = countableComments.Any(x => x.Type == CommentType.Unrepairable),
                        FfmpegDetected = ffmpegDetectionRule.LavfEncoderDetected && ffmpegDetectionRule.EndTagDetected,

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

                return new CommandResponse<FixResponse> { Status = ResponseStatus.OK, Data = response };
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new CommandResponse<FixResponse> { Status = ResponseStatus.Cancelled };
            }
            catch (NotFlvFileException ex)
            {
                return new CommandResponse<FixResponse>
                {
                    Status = ResponseStatus.NotFlvFile,
                    Exception = ex,
                    ErrorMessage = ex.Message
                };
            }
            catch (UnknownFlvTagTypeException ex)
            {
                return new CommandResponse<FixResponse>
                {
                    Status = ResponseStatus.UnknownFlvTagType,
                    Exception = ex,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new CommandResponse<FixResponse>
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

        private class AutoFixFlvWriterTargetProvider : IFlvWriterTargetProvider
        {
            private readonly string pathTemplate;
            private int fileIndex = 1;

            public event EventHandler<string>? BeforeFileOpen;

            public AutoFixFlvWriterTargetProvider(string pathTemplate)
            {
                this.pathTemplate = pathTemplate;
            }

            public Stream CreateAccompanyingTextLogStream()
            {
                var path = Path.ChangeExtension(this.pathTemplate, "txt");
                return new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            }

            public (Stream stream, object? state) CreateOutputStream()
            {
                var i = this.fileIndex++;
                var path = Path.ChangeExtension(this.pathTemplate, $"fix_p{i:D3}.flv");
                var fileStream = File.Open(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                BeforeFileOpen?.Invoke(this, path);
                return (fileStream, null);
            }
        }
    }
}
