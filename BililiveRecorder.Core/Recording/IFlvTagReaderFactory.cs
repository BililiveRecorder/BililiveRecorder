using System.IO.Pipelines;
using BililiveRecorder.Flv;

namespace BililiveRecorder.Core.Recording
{
    public interface IFlvTagReaderFactory
    {
        IFlvTagReader CreateFlvTagReader(PipeReader pipeReader);
    }
}
