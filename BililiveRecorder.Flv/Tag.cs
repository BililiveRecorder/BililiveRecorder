using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
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

        [XmlAttribute]
        public string? DataHash { get; set; }

        [XmlIgnore]
        public MemoryStream? BinaryData { get; set; }

        [XmlElement]
        public ScriptTagBody? ScriptData { get; set; }

        [XmlElement]
        public TagExtraData? ExtraData { get; set; }

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
                MemoryStream? new_stream = null;
                if (value != null)
                    new_stream = BinaryConvertUtilities.HexStringToMemoryStream(value);

                this.BinaryData?.Dispose();
                this.BinaryData = new_stream;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ShouldSerializeBinaryDataForSerializationUseOnly() => 0 != (this.Flag & TagFlag.Header);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ShouldSerializeScriptData() => this.Type == TagType.Script;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ShouldSerializeNalus() => this.Type == TagType.Video && (0 == (this.Flag & TagFlag.Header));

        public override string ToString() => this.DebuggerDisplay;

        object ICloneable.Clone() => this.Clone(null);
        public Tag Clone() => this.Clone(null);
        public Tag Clone(IMemoryStreamProvider? provider = null)
        {
            MemoryStream? binaryData = null;
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
                DataHash = this.DataHash,
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
    }
}
