using System.IO;

namespace BililiveRecorder.Flv
{
    public interface IMemoryStreamProvider
    {
        Stream CreateMemoryStream(string tag);
    }
}
