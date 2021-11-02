using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BililiveRecorder.Flv.Xml;

namespace BililiveRecorder.Flv.RuleTests
{
    public static class SampleFileLoader
    {
        public static XmlFlvFile Load(string fileName)
        {
            var stackTrace = new StackTrace();
            var frames = stackTrace.GetFrames();
            var attr = frames.Select(x => x!.GetMethod()!.GetCustomAttribute<SampleFileTestDataAttribute>()).First(x => x is not null);
            var fullPath = Path.Combine(attr!.BasePath, fileName);

            using var s = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return (XmlFlvFile)XmlFlvFile.Serializer.Deserialize(s)!;
        }
    }
}
