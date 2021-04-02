using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using BililiveRecorder.Flv.Amf;

namespace BililiveRecorder.Flv
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class Tag : ICloneable
    {
        [XmlAttribute]
        public TagType Type { get; set; }

        [XmlAttribute, DefaultValue(TagFlag.None)]
        public TagFlag Flag { get; set; }

        [XmlIgnore]
        public long Index { get; set; } = -1;

        [XmlAttribute]
        public uint Size { get; set; }

        [XmlAttribute]
        public int Timestamp { get; set; }

        [XmlIgnore]
        public Stream? BinaryData { get; set; }

        [XmlElement]
        public ScriptTagBody? ScriptData { get; set; }

        [XmlElement]
        public List<H264Nalu>? Nalus { get; set; }

        [XmlElement(nameof(BinaryData))]
        public string? BinaryDataForSerializationUseOnly
        {
            get => this.BinaryData == null
                ? null
                : BinaryConvertUtilities.StreamToHexString(this.BinaryData);
            set
            {
                Stream? new_stream = null;
                if (value != null)
                    new_stream = BinaryConvertUtilities.HexStringToMemoryStream(value);

                this.BinaryData?.Dispose();
                this.BinaryData = new_stream;
            }
        }

        public bool ShouldSerializeBinaryDataForSerializationUseOnly() => 0 != (this.Flag & TagFlag.Header);

        public bool ShouldSerializeScriptData() => this.Type == TagType.Script;

        public bool ShouldSerializeNalus() => this.Type == TagType.Video && (0 == (this.Flag & TagFlag.Header));

        public override string ToString() => this.DebuggerDisplay;

        object ICloneable.Clone() => this.Clone(null);
        public Tag Clone() => this.Clone(null);
        public Tag Clone(IMemoryStreamProvider? provider = null)
        {
            Stream? binaryData = null;
            if (this.BinaryData != null)
            {
                binaryData = provider?.CreateMemoryStream(nameof(Tag) + ":" + nameof(Clone)) ?? new MemoryStream();
                this.BinaryData.Seek(0, SeekOrigin.Begin);
                this.BinaryData.CopyToAsync(binaryData);
            }

            ScriptTagBody? scriptData = null;
            if (this.ScriptData != null)
            {
                using var stream = provider?.CreateMemoryStream(nameof(Tag) + ":" + nameof(Clone) + ":Temp") ?? new MemoryStream();
                this.ScriptData.WriteTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
                scriptData = ScriptTagBody.Parse(stream);
            }

            return new Tag
            {
                Type = this.Type,
                Flag = this.Flag,
                Size = this.Size,
                Index = this.Index,
                Timestamp = this.Timestamp,
                BinaryData = binaryData,
                ScriptData = scriptData,
                Nalus = this.Nalus is null ? null : new List<H264Nalu>(this.Nalus),
            };
        }

        private string DebuggerDisplay => string.Format("{0}, {1}{2}{3}, TS={4}, Size={5}",
            this.Type switch
            {
                TagType.Audio => "A",
                TagType.Video => "V",
                TagType.Script => "S",
                _ => "?",
            },
            this.Flag.HasFlag(TagFlag.Keyframe) ? "K" : "-",
            this.Flag.HasFlag(TagFlag.Header) ? "H" : "-",
            this.Flag.HasFlag(TagFlag.End) ? "E" : "-",
            this.Timestamp,
            this.Size);

        private static class BinaryConvertUtilities
        {
            private static readonly uint[] _lookup32 = CreateLookup32();

            private static uint[] CreateLookup32()
            {
                var result = new uint[256];
                for (var i = 0; i < 256; i++)
                {
                    var s = i.ToString("X2");
                    result[i] = s[0] + ((uint)s[1] << 16);
                }
                return result;
            }

            internal static string ByteArrayToHexString(byte[] bytes)
            {
                var lookup32 = _lookup32;
                var result = new char[bytes.Length * 2];
                for (var i = 0; i < bytes.Length; i++)
                {
                    var val = lookup32[bytes[i]];
                    result[2 * i] = (char)val;
                    result[2 * i + 1] = (char)(val >> 16);
                }
                return new string(result);
            }

            internal static byte[] HexStringToByteArray(string hex)
            {
                var bytes = new byte[hex.Length / 2];
                for (var i = 0; i < hex.Length; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }

            internal static string StreamToHexString(Stream stream)
            {
                var lookup32 = _lookup32;
                stream.Seek(0, SeekOrigin.Begin);
                var result = new char[stream.Length * 2];
                for (var i = 0; i < stream.Length; i++)
                {
                    var val = lookup32[stream.ReadByte()];
                    result[2 * i] = (char)val;
                    result[2 * i + 1] = (char)(val >> 16);
                }
                return new string(result);
            }

            internal static Stream HexStringToMemoryStream(string hex)
            {
                var stream = new MemoryStream(hex.Length / 2);
                for (var i = 0; i < hex.Length; i += 2)
                    stream.WriteByte(Convert.ToByte(hex.Substring(i, 2), 16));
                return stream;
            }
        }
    }
}
