using System;
using System.Collections.Generic;
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

        public ScriptTagBody()
        {
            this.Values = new List<IScriptDataValue>();
        }

        public ScriptTagBody(List<IScriptDataValue> values)
        {
            this.Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public List<IScriptDataValue> Values { get; set; }

        public static ScriptTagBody Parse(string json) =>
            new ScriptTagBody(JsonConvert.DeserializeObject<List<IScriptDataValue>>(json, jsonSerializerSettings)
                ?? throw new Exception("JsonConvert.DeserializeObject returned null"));

        public string ToJson() => JsonConvert.SerializeObject(this.Values, jsonSerializerSettings);

        public static ScriptTagBody Parse(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            return Parse(ms);
        }

        public static ScriptTagBody Parse(Stream stream) => Parse(new BigEndianBinaryReader(stream, Encoding.UTF8, true));

        public static ScriptTagBody Parse(BigEndianBinaryReader binaryReader)
        {
            var list = new List<IScriptDataValue>();

            while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                list.Add(ParseValue(binaryReader));

            return new ScriptTagBody(list);
        }

        public byte[] ToBytes()
        {
            using var ms = new MemoryStream();
            this.WriteTo(ms);
            return ms.ToArray();
        }

        public void WriteTo(Stream stream)
        {
            foreach (var value in this.Values)
            {
                value.WriteTo(stream);
            }
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
                            str = str.Replace("\0", "");
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
                return Encoding.UTF8.GetString(binaryReader.ReadBytes(length)).Replace("\0", ""); ;
            }
        }

        XmlSchema IXmlSerializable.GetSchema() => null!;
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            var str = reader.ReadElementContentAsString();
            var obj = Parse(str);
            this.Values = obj.Values;
        }
        void IXmlSerializable.WriteXml(XmlWriter writer) => writer.WriteString(this.ToJson());
    }
}
