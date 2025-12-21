using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace BililiveRecorder.Flv.Amf
{
    [DebuggerDisplay("AmfLongString, {Value}")]
    public class ScriptDataLongString : IScriptDataValue
    {
        public ScriptDataType Type => ScriptDataType.LongString;

        [JsonProperty(Required = Required.Always)]
        public string Value { get; set; } = string.Empty;

        public void WriteTo(Stream stream)
        {
            var bytes = Encoding.UTF8.GetBytes(this.Value);

            var buffer = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)bytes.Length);

            stream.WriteByte((byte)this.Type);
            stream.Write(buffer);
            stream.Write(bytes);
        }

        public override bool Equals(object? obj) => obj is ScriptDataLongString @string && this.Value == @string.Value;
        public override int GetHashCode() => HashCode.Combine(this.Value);
        public static bool operator ==(ScriptDataLongString left, ScriptDataLongString right) => EqualityComparer<ScriptDataLongString>.Default.Equals(left, right);
        public static bool operator !=(ScriptDataLongString left, ScriptDataLongString right) => !(left == right);
        public static implicit operator string(ScriptDataLongString @string) => @string.Value;
        public static implicit operator ScriptDataLongString(string @string) => new ScriptDataLongString { Value = @string };
        public static implicit operator ScriptDataLongString(ScriptDataString @string) => new ScriptDataLongString { Value = @string.Value };
    }
}
