using System;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv
{
    public interface IFlvProcessingContextWriter : IDisposable
    {
        Action<ScriptTagBody>? BeforeScriptTagWrite { get; set; }
        Action<ScriptTagBody>? BeforeScriptTagRewrite { get; set; }

        event EventHandler<FileClosedEventArgs> FileClosed;

        Task<int> WriteAsync(FlvProcessingContext context);
    }
}
