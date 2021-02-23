using System.Collections.Generic;
using System.IO;

namespace BililiveRecorder.Flv
{
    public interface IFlvWriterTargetProvider
    {
        (Stream stream, object state) CreateOutputStream();

        Stream CreateAlternativeHeaderStream();

        bool ShouldCreateNewFile(Stream outputStream, IList<Tag> tags);
    }
}
