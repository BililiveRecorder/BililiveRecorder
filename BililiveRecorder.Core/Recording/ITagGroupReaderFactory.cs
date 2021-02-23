using BililiveRecorder.Flv;

namespace BililiveRecorder.Core.Recording
{
    public interface ITagGroupReaderFactory
    {
        ITagGroupReader CreateTagGroupReader(IFlvTagReader flvTagReader);
    }
}
