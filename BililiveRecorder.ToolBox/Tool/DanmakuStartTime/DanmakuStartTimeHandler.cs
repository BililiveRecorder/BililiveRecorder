using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace BililiveRecorder.ToolBox.Tool.DanmakuStartTime
{
    public class DanmakuStartTimeHandler : ICommandHandler<DanmakuStartTimeRequest, DanmakuStartTimeResponse>
    {
        public string Name => "Read Danmaku start_time";

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<CommandResponse<DanmakuStartTimeResponse>> Handle(DanmakuStartTimeRequest request, CancellationToken cancellationToken, ProgressCallback? progress)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            List<DanmakuStartTimeResponse.DanmakuStartTime> result = new();

            try
            {
                progress?.Invoke(0);
                var finished = 0;
                double total = request.Inputs.Length;

                Parallel.ForEach(request.Inputs, input =>
                {
                    try
                    {
                        using var file = File.Open(input, FileMode.Open, FileAccess.Read, FileShare.Read);
                        var r = XmlReader.Create(file, null);
                        r.ReadStartElement("i");
                        while (r.Name != "i")
                        {
                            if (r.Name == "BililiveRecorderRecordInfo")
                            {
                                var el = (XNode.ReadFrom(r) as XElement)!;
                                var time = (DateTimeOffset)el.Attribute("start_time");

                                lock (result)
                                    result.Add(new DanmakuStartTimeResponse.DanmakuStartTime { Path = input, StartTime = time });

                                Interlocked.Increment(ref finished);

                                progress?.Invoke(finished / total);
                                break;
                            }
                            else
                            {
                                r.Skip();
                            }
                        }
                    }
                    catch (Exception) { }
                });
            }
            catch (Exception ex)
            {
                return new CommandResponse<DanmakuStartTimeResponse>
                {
                    Status = ResponseStatus.Error,
                    Exception = ex,
                    ErrorMessage = ex.Message
                };
            }

            return new CommandResponse<DanmakuStartTimeResponse> { Status = ResponseStatus.OK, Data = new DanmakuStartTimeResponse { StartTimes = result.ToArray() } };
        }
    }
}
