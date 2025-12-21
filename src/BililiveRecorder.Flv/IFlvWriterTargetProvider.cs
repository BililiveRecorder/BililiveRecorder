using System.IO;

namespace BililiveRecorder.Flv
{
    public interface IFlvWriterTargetProvider
    {
        (Stream stream, object? state) CreateOutputStream();

        Stream CreateAccompanyingTextLogStream();
    }
}
