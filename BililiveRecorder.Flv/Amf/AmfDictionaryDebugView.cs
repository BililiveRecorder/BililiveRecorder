using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BililiveRecorder.Flv.Amf
{
    internal sealed class AmfDictionaryDebugView
    {
        private readonly IDictionary<string, IScriptDataValue> _dict;

        public AmfDictionaryDebugView(IDictionary<string, IScriptDataValue> dictionary)
        {
            this._dict = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePairDebugView<string, IScriptDataValue>[] Items
            => this._dict.Select(x => new KeyValuePairDebugView<string, IScriptDataValue>(x.Key, x.Value)).ToArray();
    }
}
