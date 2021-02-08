using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace BililiveRecorder.Flv.Amf
{
    [DebuggerDisplay("AmfDate, {Value}")]
    public class ScriptDataDate : IScriptDataValue
    {
        public ScriptDataDate() { }
        public ScriptDataDate(DateTimeOffset value)
        {
            this.Value = value;
        }

        public ScriptDataDate(double dateTime, short localDateTimeOffset)
        {
            this.Value = DateTimeOffset.FromUnixTimeMilliseconds((long)dateTime).ToOffset(TimeSpan.FromMinutes(localDateTimeOffset));
        }

        public ScriptDataType Type => ScriptDataType.Date;

        [JsonProperty]
        public DateTimeOffset Value { get; set; }

        public void WriteTo(Stream stream)
        {
            var dateTime = (double)this.Value.ToUnixTimeMilliseconds();
            var localDateTimeOffset = (short)this.Value.Offset.TotalMinutes;
            var buffer1 = new byte[sizeof(double)];
            var buffer2 = new byte[sizeof(ushort)];
            BinaryPrimitives.WriteInt64BigEndian(buffer1, BitConverter.DoubleToInt64Bits(dateTime));
            BinaryPrimitives.WriteInt16BigEndian(buffer2, localDateTimeOffset);
            stream.WriteByte((byte)this.Type);
            stream.Write(buffer1);
            stream.Write(buffer2);
        }

        public override bool Equals(object? obj) => obj is ScriptDataDate date && this.Value.Equals(date.Value);
        public override int GetHashCode() => HashCode.Combine(this.Value);
        public static bool operator ==(ScriptDataDate left, ScriptDataDate right) => EqualityComparer<ScriptDataDate>.Default.Equals(left, right);
        public static bool operator !=(ScriptDataDate left, ScriptDataDate right) => !(left == right);
        public static implicit operator DateTimeOffset(ScriptDataDate date) => date.Value;
        public static implicit operator ScriptDataDate(DateTimeOffset date) => new ScriptDataDate(date);
        public static implicit operator DateTime(ScriptDataDate date) => date.Value.DateTime;
        public static implicit operator ScriptDataDate(DateTime date) => new ScriptDataDate(date);
    }
}
