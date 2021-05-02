using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BililiveRecorder.Flv.Xml
{
    [XmlRoot("BililiveRecorderFlv")]
    public class XmlFlvFile
    {
        public static XmlSerializer Serializer { get; } = new XmlSerializer(typeof(XmlFlvFile));

        public XmlFlvFileMeta? Meta { get; set; }

        public List<Tag> Tags { get; set; } = new List<Tag>();

        public class XmlFlvFileMeta
        {
            public string? Version { get; set; }

            [XmlElement(nameof(ExportTime))]
            public string ExportTimeForXml
            {
                get => this.ExportTime.ToString("o");
                set => this.ExportTime = DateTimeOffset.Parse(value);
            }

            [XmlIgnore]
            public DateTimeOffset ExportTime { get; set; }

            public long FileSize { get; set; }

            [XmlElement(nameof(FileCreationTime))]
            public string FileCreationTimeForXml
            {
                get => this.FileCreationTime.ToString("o");
                set => this.FileCreationTime = DateTimeOffset.Parse(value);
            }

            [XmlIgnore]
            public DateTimeOffset FileCreationTime { get; set; }

            [XmlElement(nameof(FileModificationTime))]
            public string FileModificationTimeForXml
            {
                get => this.FileModificationTime.ToString("o");
                set => this.FileModificationTime = DateTimeOffset.Parse(value);
            }

            [XmlIgnore]
            public DateTimeOffset FileModificationTime { get; set; }
        }
    }
}
