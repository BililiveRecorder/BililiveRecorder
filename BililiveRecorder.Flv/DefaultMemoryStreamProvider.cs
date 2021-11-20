using System.IO;

namespace BililiveRecorder.Flv
{
    public class DefaultMemoryStreamProvider : IMemoryStreamProvider
    {
        public MemoryStream CreateMemoryStream(string tag) => new MemoryStream();
    }
}
