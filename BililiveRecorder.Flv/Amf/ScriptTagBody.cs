using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using BililiveRecorder.Flv.Parser;
using Newtonsoft.Json;

namespace BililiveRecorder.Flv.Amf
{
    public class ScriptTagBody : IXmlSerializable
    {
        private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.DateTimeOffset,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
        };

        public ScriptDataString Name { get; set; } = string.Empty;

        public ScriptDataObject Value { get; set; } = new ScriptDataObject();

        public static ScriptTagBody Parse(string json) => JsonConvert.DeserializeObject<ScriptTagBody>(json, jsonSerializerSettings)!;

        public string ToJson() => JsonConvert.SerializeObject(this, jsonSerializerSettings);

        public static ScriptTagBody Parse(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            return Parse(ms);
        }

        public static ScriptTagBody Parse(Stream stream)
        {
            return Parse(new BigEndianBinaryReader(stream, Encoding.UTF8, true));
        }

        public static ScriptTagBody Parse(BigEndianBinaryReader binaryReader)
        {
            if (ParseValue(binaryReader) is ScriptDataString stringName)
                return new ScriptTagBody
                {
                    Name = stringName,
                    Value = ((ParseValue(binaryReader)) switch
                    {
                        ScriptDataEcmaArray value => value,
                        ScriptDataObject value => value,
                        _ => throw new AmfException("type of ScriptTagBody.Value is not supported"),
                    })
                };
            else
                throw new AmfException("ScriptTagBody.Name is not String");
        }

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream();
            this.WriteTo(ms);
            return ms.ToArray();
        }

        public void WriteTo(Stream stream)
        {
            this.Name.WriteTo(stream);
            this.Value.WriteTo(stream);
        }

        public static IScriptDataValue ParseValue(BigEndianBinaryReader binaryReader)
        {
            var type = (ScriptDataType)binaryReader.ReadByte();
            switch (type)
            {
                case ScriptDataType.Number:
                    return (ScriptDataNumber)binaryReader.ReadDouble();
                case ScriptDataType.Boolean:
                    return (ScriptDataBoolean)binaryReader.ReadBoolean();
                case ScriptDataType.String:
                    return ReadScriptDataString(binaryReader, false) ?? string.Empty;
                case ScriptDataType.Object:
                    {
                        var result = new ScriptDataObject();
                        while (true)
                        {
                            var propertyName = ReadScriptDataString(binaryReader, true);
                            if (propertyName is null)
                                break;

                            var propertyData = ParseValue(binaryReader);

                            result[propertyName] = propertyData;
                        }
                        return result;
                    }
                case ScriptDataType.MovieClip:
                    throw new AmfException("MovieClip is not supported");
                case ScriptDataType.Null:
                    return new ScriptDataNull();
                case ScriptDataType.Undefined:
                    return new ScriptDataUndefined();
                case ScriptDataType.Reference:
                    return (ScriptDataReference)binaryReader.ReadUInt16();
                case ScriptDataType.EcmaArray:
                    {
                        binaryReader.ReadUInt32();
                        var result = new ScriptDataEcmaArray();
                        while (true)
                        {
                            var propertyName = ReadScriptDataString(binaryReader, true);
                            if (propertyName is null)
                                break;

                            var propertyData = ParseValue(binaryReader);

                            result[propertyName] = propertyData;
                        }
                        return result;
                    }
                case ScriptDataType.ObjectEndMarker:
                    throw new AmfException("Read ObjectEndMarker");
                case ScriptDataType.StrictArray:
                    {
                        var length = binaryReader.ReadUInt32();
                        var result = new ScriptDataStrictArray();
                        for (var i = 0; i < length; i++)
                        {
                            var value = ParseValue(binaryReader);
                            result.Add(value);
                        }
                        return result;
                    }
                case ScriptDataType.Date:
                    {
                        var dateTime = binaryReader.ReadDouble();
                        var offset = binaryReader.ReadInt16();
                        return new ScriptDataDate(dateTime, offset);
                    }
                case ScriptDataType.LongString:
                    {
                        var length = binaryReader.ReadUInt32();
                        if (length > int.MaxValue)
                            throw new AmfException($"LongString larger than {int.MaxValue} is not supported.");
                        else
                        {
                            var bytes = binaryReader.ReadBytes((int)length);
                            var str = Encoding.UTF8.GetString(bytes);
                            return (ScriptDataLongString)str;
                        }
                    }
                default:
                    throw new AmfException("Unknown ScriptDataValueType");
            }

            static ScriptDataString? ReadScriptDataString(BigEndianBinaryReader binaryReader, bool expectObjectEndMarker)
            {
                var length = binaryReader.ReadUInt16();
                if (length == 0)
                {
                    if (expectObjectEndMarker && binaryReader.ReadByte() != 9)
                        throw new AmfException("ObjectEndMarker not matched.");
                    return null;
                }
                return Encoding.UTF8.GetString(binaryReader.ReadBytes(length));
            }
        }

        XmlSchema IXmlSerializable.GetSchema() => null!;
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            var str = reader.ReadElementContentAsString();
            var obj = Parse(str);
            this.Name = obj.Name;
            this.Value = obj.Value;
        }
        void IXmlSerializable.WriteXml(XmlWriter writer) => writer.WriteString(this.ToJson());
    }
}
