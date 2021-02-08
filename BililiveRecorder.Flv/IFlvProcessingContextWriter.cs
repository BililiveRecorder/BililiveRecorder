using System;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv
{
    public interface IFlvProcessingContextWriter : IDisposable
    {
        Task WriteAsync(FlvProcessingContext context);
    }
}
