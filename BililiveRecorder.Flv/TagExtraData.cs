using System.Xml.Serialization;

namespace BililiveRecorder.Flv
{
    public sealed class TagExtraData
    {
        public TagExtraData()
        {
        }

        [XmlAttribute]
        public string FirstBytes { get; set; } = string.Empty;

        [XmlAttribute]
        public int CompositionTime { get; set; }

        [XmlAttribute]
        public int FinalTime { get; set; }

        public bool ShouldSerializeCompositionTime() => this.CompositionTime != int.MinValue;

        public bool ShouldSerializeFinalTime() => this.CompositionTime != int.MinValue;
    }
}
