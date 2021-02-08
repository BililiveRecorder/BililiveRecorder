using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace BililiveRecorder.Flv.Amf
{
    [DebuggerDisplay("AmfReference, {Value}")]
    public class ScriptDataReference : IScriptDataValue
    {
        public ScriptDataType Type => ScriptDataType.Reference;

        [JsonProperty]
        public ushort Value { get; set; }

        public void WriteTo(Stream stream)
        {
            stream.WriteByte((byte)this.Type);

            var buffer = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, this.Value);
            stream.Write(buffer);
        }

        public override bool Equals(object? obj) => obj is ScriptDataReference reference && this.Value == reference.Value;
        public override int GetHashCode() => HashCode.Combine(this.Value);
        public static bool operator ==(ScriptDataReference left, ScriptDataReference right) => EqualityComparer<ScriptDataReference>.Default.Equals(left, right);
        public static bool operator !=(ScriptDataReference left, ScriptDataReference right) => !(left == right);
        public static implicit operator ushort(ScriptDataReference reference) => reference.Value;
        public static implicit operator ScriptDataReference(ushort number) => new ScriptDataReference { Value = number };
    }
}
