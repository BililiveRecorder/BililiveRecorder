using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace BililiveRecorder.Flv.Amf
{
    [DebuggerDisplay("AmfBoolean, {Value}")]
    public class ScriptDataBoolean : IScriptDataValue
    {
        public ScriptDataType Type => ScriptDataType.Boolean;

        [JsonProperty]
        public bool Value { get; set; }

        public void WriteTo(Stream stream)
        {
            stream.WriteByte((byte)this.Type);
            stream.WriteByte((byte)(this.Value ? 1 : 0));
        }

        public override bool Equals(object? obj) => obj is ScriptDataBoolean boolean && this.Value == boolean.Value;
        public override int GetHashCode() => HashCode.Combine(this.Value);
        public static bool operator ==(ScriptDataBoolean left, ScriptDataBoolean right) => EqualityComparer<ScriptDataBoolean>.Default.Equals(left, right);
        public static bool operator !=(ScriptDataBoolean left, ScriptDataBoolean right) => !(left == right);
        public static implicit operator bool(ScriptDataBoolean boolean) => boolean.Value;
        public static implicit operator ScriptDataBoolean(bool boolean) => new ScriptDataBoolean { Value = boolean };
    }
}
