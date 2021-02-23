using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Grouping;

namespace BililiveRecorder.Core.Recording
{
    public class TagGroupReaderFactory : ITagGroupReaderFactory
    {
        public ITagGroupReader CreateTagGroupReader(IFlvTagReader flvTagReader) =>
            new TagGroupReader(flvTagReader);
    }
}
