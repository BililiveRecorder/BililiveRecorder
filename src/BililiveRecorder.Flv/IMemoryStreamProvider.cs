using System.IO;

namespace BililiveRecorder.Flv
{
    public interface IMemoryStreamProvider
    {
        MemoryStream CreateMemoryStream(string tag);
    }
}
