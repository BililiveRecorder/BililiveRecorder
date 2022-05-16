using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Grouping;

namespace BililiveRecorder.Core.Recording
{
    internal class TagGroupReaderFactory : ITagGroupReaderFactory
    {
        public ITagGroupReader CreateTagGroupReader(IFlvTagReader flvTagReader) =>
            new TagGroupReader(flvTagReader);
    }
}
