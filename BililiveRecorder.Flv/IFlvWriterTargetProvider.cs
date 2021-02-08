using System.IO;

namespace BililiveRecorder.Flv
{
    public interface IFlvWriterTargetProvider
    {
        Stream CreateOutputStream();

        Stream CreateAlternativeHeaderStream();
    }
}
