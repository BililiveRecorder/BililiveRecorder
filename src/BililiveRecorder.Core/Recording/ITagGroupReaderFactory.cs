using BililiveRecorder.Flv;

namespace BililiveRecorder.Core.Recording
{
    internal interface ITagGroupReaderFactory
    {
        ITagGroupReader CreateTagGroupReader(IFlvTagReader flvTagReader);
    }
}
