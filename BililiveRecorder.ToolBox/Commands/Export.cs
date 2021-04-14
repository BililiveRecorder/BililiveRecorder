using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
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

        public Task<ExportResponse> Handle(ExportRequest request) => this.Handle(request, null);

        public async Task<ExportResponse> Handle(ExportRequest request, Func<double, Task>? progress)
        {
            using var inputStream = File.OpenRead(request.Input);
            using var outputStream = File.OpenWrite(request.Output);

            var tags = new List<Tag>();

            {
                using var reader = new FlvTagPipeReader(PipeReader.Create(inputStream), new DefaultMemoryStreamProvider(), skipData: true, logger: logger);
                var count = 0;
                while (true)
                {
                    var tag = await reader.ReadTagAsync(default).ConfigureAwait(false);
                    if (tag is null) break;
                    tags.Add(tag);

                    if (count++ % 300 == 0)
                        progress?.Invoke((double)inputStream.Position / inputStream.Length);
                }
            }

            {
                using var writer = new StreamWriter(new GZipStream(outputStream, CompressionLevel.Optimal));
                XmlFlvFile.Serializer.Serialize(writer, new XmlFlvFile
                {
                    Tags = tags
                });
            }

            return new ExportResponse();
        }

        public void PrintResponse(ExportResponse response)
        { }
    }
}
