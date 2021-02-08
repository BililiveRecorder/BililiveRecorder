using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace BililiveRecorder.Flv.Amf
{
    [DebuggerDisplay("AmfString, {Value}")]
    public class ScriptDataString : IScriptDataValue
    {
        public ScriptDataType Type => ScriptDataType.String;

        [JsonProperty(Required = Required.Always)]
        public string Value { get; set; } = string.Empty;

        public void WriteTo(Stream stream)
        {
            var bytes = Encoding.UTF8.GetBytes(this.Value);
            if (bytes.Length > ushort.MaxValue)
                throw new AmfException($"Cannot write more than {ushort.MaxValue} into ScriptDataString");

            var buffer = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)bytes.Length);

            stream.WriteByte((byte)this.Type);
            stream.Write(buffer);
            stream.Write(bytes);
        }

        public override bool Equals(object? obj) => obj is ScriptDataString @string && this.Value == @string.Value;
        public override int GetHashCode() => HashCode.Combine(this.Value);
        public static bool operator ==(ScriptDataString left, ScriptDataString right) => EqualityComparer<ScriptDataString>.Default.Equals(left, right);
        public static bool operator !=(ScriptDataString left, ScriptDataString right) => !(left == right);
        public static implicit operator string(ScriptDataString @string) => @string.Value;
        public static implicit operator ScriptDataString(string @string) => new ScriptDataString { Value = @string };
        public static implicit operator ScriptDataString(ScriptDataLongString @string) => new ScriptDataString { Value = @string.Value };
    }
}
