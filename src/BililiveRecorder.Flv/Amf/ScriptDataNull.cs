using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BililiveRecorder.Flv.Amf
{
    [DebuggerDisplay("AmfNull")]
    public class ScriptDataNull : IScriptDataValue
    {
        public ScriptDataType Type => ScriptDataType.Null;

        public void WriteTo(Stream stream) => stream.WriteByte((byte)this.Type);

        public override bool Equals(object? obj) => obj is ScriptDataNull;
        public override int GetHashCode() => 0;
        public static bool operator ==(ScriptDataNull left, ScriptDataNull right) => EqualityComparer<ScriptDataNull>.Default.Equals(left, right);
        public static bool operator !=(ScriptDataNull left, ScriptDataNull right) => !(left == right);
    }
}
