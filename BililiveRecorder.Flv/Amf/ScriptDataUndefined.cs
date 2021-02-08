using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BililiveRecorder.Flv.Amf
{
    [DebuggerDisplay("AmfUndefined")]
    public class ScriptDataUndefined : IScriptDataValue
    {
        public ScriptDataType Type => ScriptDataType.Undefined;

        public void WriteTo(Stream stream) => stream.WriteByte((byte)this.Type);

        public override bool Equals(object? obj) => obj is ScriptDataUndefined;
        public override int GetHashCode() => 0;
        public static bool operator ==(ScriptDataUndefined left, ScriptDataUndefined right) => EqualityComparer<ScriptDataUndefined>.Default.Equals(left, right);
        public static bool operator !=(ScriptDataUndefined left, ScriptDataUndefined right) => !(left == right);
    }
}
