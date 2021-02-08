using System.IO;

namespace BililiveRecorder.Flv
{
    public class DefaultMemoryStreamProvider : IMemoryStreamProvider
    {
        public Stream CreateMemoryStream(string tag) => new MemoryStream();
    }
}
