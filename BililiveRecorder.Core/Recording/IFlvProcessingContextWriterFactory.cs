using BililiveRecorder.Flv;

namespace BililiveRecorder.Core.Recording
{
    public interface IFlvProcessingContextWriterFactory
    {
        IFlvProcessingContextWriter CreateWriter(IFlvWriterTargetProvider targetProvider);
    }
}
