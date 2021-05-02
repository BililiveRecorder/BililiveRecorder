using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Xml;
using Serilog;

namespace BililiveRecorder.ToolBox.Commands
{
    public class ExportRequest : ICommandRequest<ExportResponse>
    {
        public string Input { get; set; } = string.Empty;

        public string Output { get; set; } = string.Empty;
    }

    public class ExportResponse
    {
    }

    public class ExportHandler : ICommandHandler<ExportRequest, ExportResponse>
    {
        private static readonly ILogger logger = Log.ForContext<ExportHandler>();

        public Task<CommandResponse<ExportResponse>> Handle(ExportRequest request) => this.Handle(request, default, null);

        public async Task<CommandResponse<ExportResponse>> Handle(ExportRequest request, CancellationToken cancellationToken, Func<double, Task>? progress)
        {
            FileStream? inputStream = null, outputStream = null;
            try
            {
                XmlFlvFile.XmlFlvFileMeta meta;
                try
                {
                    var fi = new FileInfo(request.Input);
                    meta = new XmlFlvFile.XmlFlvFileMeta
                    {
                        ExportTime = DateTimeOffset.Now,
                        Version = GitVersionInformation.InformationalVersion,
                        FileSize = fi.Length,
                        FileCreationTime = fi.CreationTime,
                        FileModificationTime = fi.LastWriteTime
                    };
                    inputStream = File.Open(request.Input, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                catch (Exception ex)
                {
                    return new CommandResponse<ExportResponse>
                    {
                        Status = ResponseStatus.InputIOError,
                        Exception = ex,
                        ErrorMessage = ex.Message
                    };
                }

                try
                {
                    outputStream = File.OpenWrite(request.Output);
                }
                catch (Exception ex)
                {
                    return new CommandResponse<ExportResponse>
                    {
                        Status = ResponseStatus.OutputIOError,
                        Exception = ex,
                        ErrorMessage = ex.Message
                    };
                }

                var tags = await Task.Run(async () =>
                {
                    var count = 0;
                    var tags = new List<Tag>();
                    var memoryStreamProvider = new RecyclableMemoryStreamProvider();
                    using var reader = new FlvTagPipeReader(PipeReader.Create(inputStream), memoryStreamProvider, skipData: true, logger: logger);
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var tag = await reader.ReadTagAsync(cancellationToken).ConfigureAwait(false);
                        if (tag is null) break;
                        tags.Add(tag);

                        if (count++ % 300 == 0 && progress is not null)
                            await progress((double)inputStream.Position / inputStream.Length);
                    }
                    return tags;
                });

                if (cancellationToken.IsCancellationRequested)
                    return new CommandResponse<ExportResponse> { Status = ResponseStatus.Cancelled };

                await Task.Run(() =>
                {
                    using var writer = new StreamWriter(new GZipStream(outputStream, CompressionLevel.Optimal));
                    XmlFlvFile.Serializer.Serialize(writer, new XmlFlvFile
                    {
                        Tags = tags,
                        Meta = meta
                    });
                });

                return new CommandResponse<ExportResponse> { Status = ResponseStatus.OK, Result = new ExportResponse() };
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new CommandResponse<ExportResponse> { Status = ResponseStatus.Cancelled };
            }
            catch (NotFlvFileException ex)
            {
                return new CommandResponse<ExportResponse>
                {
                    Status = ResponseStatus.NotFlvFile,
                    Exception = ex,
                    ErrorMessage = ex.Message
                };
            }
            catch (UnknownFlvTagTypeException ex)
            {
                return new CommandResponse<ExportResponse>
                {
                    Status = ResponseStatus.UnknownFlvTagType,
                    Exception = ex,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new CommandResponse<ExportResponse>
                {
                    Status = ResponseStatus.Error,
                    Exception = ex,
                    ErrorMessage = ex.Message
                };
            }
            finally
            {
                inputStream?.Dispose();
                outputStream?.Dispose();
            }
        }

        public void PrintResponse(ExportResponse response) => Console.WriteLine("OK");
    }
}
