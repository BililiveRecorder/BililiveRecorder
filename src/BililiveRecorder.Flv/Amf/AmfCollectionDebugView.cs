using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BililiveRecorder.Flv.Amf
{
    internal sealed class AmfCollectionDebugView
    {
        private readonly ICollection<IScriptDataValue> _collection;

        public AmfCollectionDebugView(ICollection<IScriptDataValue> collection)
        {
            this._collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IScriptDataValue[] Items => this._collection.ToArray();
    }
}
