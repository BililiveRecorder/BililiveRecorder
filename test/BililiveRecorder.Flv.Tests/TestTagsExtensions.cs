using System.Collections.Generic;
using System.IO;
using BililiveRecorder.Flv.Xml;

namespace BililiveRecorder.Flv.Tests
{
    internal static class TestTagsExtensions
    {
        public static string SerializeXml(this List<Tag> tags)
        {
            using var sw = new StringWriter();
            XmlFlvFile.Serializer.Serialize(sw, new XmlFlvFile { Tags = tags });
            return sw.ToString();
        }
    }
}
