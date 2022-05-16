using System.IO.Pipelines;
using BililiveRecorder.Flv;

namespace BililiveRecorder.Core.Recording
{
    internal interface IFlvTagReaderFactory
    {
        IFlvTagReader CreateFlvTagReader(PipeReader pipeReader);
    }
}
