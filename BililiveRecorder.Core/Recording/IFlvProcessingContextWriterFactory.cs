using BililiveRecorder.Flv;

namespace BililiveRecorder.Core.Recording
{
    internal interface IFlvProcessingContextWriterFactory
    {
        IFlvProcessingContextWriter CreateWriter(IFlvWriterTargetProvider targetProvider);
    }
}
