using System.Collections.Generic;
using System.Xml.Serialization;

namespace BililiveRecorder.Flv.Xml
{
    [XmlRoot("BililiveRecorderFlv")]
    public class XmlFlvFile
    {
        public static XmlSerializer Serializer { get; } = new XmlSerializer(typeof(XmlFlvFile));

        public List<Tag> Tags { get; set; } = new List<Tag>();
    }
}
