using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace BililiveRecorder.Flv.Amf
{
    [DebuggerDisplay("AmfNumber, {Value}")]
    public class ScriptDataNumber : IScriptDataValue
    {
        public ScriptDataType Type => ScriptDataType.Number;

        [JsonProperty]
        public double Value { get; set; }

        public void WriteTo(Stream stream)
        {
            stream.WriteByte((byte)this.Type);
            var buffer = new byte[sizeof(double)];
            BinaryPrimitives.WriteInt64BigEndian(buffer, BitConverter.DoubleToInt64Bits(this.Value));
            stream.Write(buffer);
        }

        public static bool operator ==(ScriptDataNumber left, ScriptDataNumber right) => EqualityComparer<ScriptDataNumber>.Default.Equals(left, right);
        public static bool operator !=(ScriptDataNumber left, ScriptDataNumber right) => !(left == right);
        public override bool Equals(object? obj) => obj is ScriptDataNumber number && this.Value == number.Value;
        public override int GetHashCode() => HashCode.Combine(this.Value);
        public static implicit operator double(ScriptDataNumber number) => number.Value;
        public static explicit operator int(ScriptDataNumber number) => (int)number.Value;
        public static implicit operator ScriptDataNumber(double number) => new ScriptDataNumber { Value = number };
        public static explicit operator ScriptDataNumber(int number) => new ScriptDataNumber { Value = number };
    }
}
