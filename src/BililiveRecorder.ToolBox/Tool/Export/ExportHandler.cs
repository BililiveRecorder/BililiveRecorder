using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Xml;
using Serilog;

namespace BililiveRecorder.ToolBox.Tool.Export
{
    public class ExportHandler : ICommandHandler<ExportRequest, ExportResponse>
    {
        private static readonly ILogger logger = Log.ForContext<ExportHandler>();

        public string Name => "Export";

        public async Task<CommandResponse<ExportResponse>> Handle(ExportRequest request, CancellationToken cancellationToken, ProgressCallback? progress)
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
                    outputStream = new FileStream(request.Output, FileMode.Create);
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
                    using var reader = new FlvTagPipeReader(PipeReader.Create(inputStream), memoryStreamProvider, skipData: false, logger: logger);
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var tag = await reader.ReadTagAsync(cancellationToken).ConfigureAwait(false);
                        if (tag is null)
                            break;

                        tag.UpdateExtraData();
                        tag.UpdateDataHash();
                        if (!tag.ShouldSerializeBinaryDataForSerializationUseOnly())
                            tag.BinaryData?.Dispose();

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
                    switch (Path.GetExtension(request.Output))
                    {
                        case ".zip":
                        default:
                            {
                                using var zip = new ZipArchive(outputStream, ZipArchiveMode.Create, false, Encoding.UTF8);
                                using var writer = XmlWriter.Create(new StreamWriter(zip.CreateEntry(Path.GetFileName(request.Input) + ".brec.xml").Open(), Encoding.UTF8), new()
                                {
                                    Encoding = Encoding.UTF8,
                                    Indent = true
                                });

                                XmlFlvFile.Serializer.Serialize(writer, new XmlFlvFile
                                {
                                    Tags = tags,
                                    Meta = meta
                                });
                            }
                            break;
                        case ".xml":
                            {
                                using var writer = XmlWriter.Create(new StreamWriter(outputStream, Encoding.UTF8), new()
                                {
                                    Encoding = Encoding.UTF8,
                                    Indent = true
                                });
                                XmlFlvFile.Serializer.Serialize(writer, new XmlFlvFile
                                {
                                    Tags = tags,
                                    Meta = meta
                                });
                            }
                            break;
                    }
                });

                return new CommandResponse<ExportResponse> { Status = ResponseStatus.OK, Data = new ExportResponse() };
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
    }
}
