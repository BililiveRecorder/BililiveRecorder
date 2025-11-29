using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace BililiveRecorder.Flv.Amf
{
    [DebuggerTypeProxy(typeof(AmfCollectionDebugView))]
    [DebuggerDisplay("AmfStrictArray, Count = {Count}")]
    public class ScriptDataStrictArray : IScriptDataValue, IList<IScriptDataValue>, ICollection<IScriptDataValue>, IEnumerable<IScriptDataValue>, IReadOnlyCollection<IScriptDataValue>, IReadOnlyList<IScriptDataValue>
    {
        public ScriptDataType Type => ScriptDataType.StrictArray;

        [JsonProperty]
        public List<IScriptDataValue> Value { get; set; } = new List<IScriptDataValue>();

        public void WriteTo(Stream stream)
        {
            stream.WriteByte((byte)this.Type);

            var buffer = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)this.Value.Count);
            stream.Write(buffer);

            foreach (var item in this.Value)
                item.WriteTo(stream);
        }

        public IScriptDataValue this[int index] { get => ((IList<IScriptDataValue>)this.Value)[index]; set => ((IList<IScriptDataValue>)this.Value)[index] = value; }
        public int Count => ((IList<IScriptDataValue>)this.Value).Count;
        public bool IsReadOnly => ((IList<IScriptDataValue>)this.Value).IsReadOnly;
        public void Add(IScriptDataValue item) => ((IList<IScriptDataValue>)this.Value).Add(item);
        public void Clear() => ((IList<IScriptDataValue>)this.Value).Clear();
        public bool Contains(IScriptDataValue item) => ((IList<IScriptDataValue>)this.Value).Contains(item);
        public void CopyTo(IScriptDataValue[] array, int arrayIndex) => ((IList<IScriptDataValue>)this.Value).CopyTo(array, arrayIndex);
        public IEnumerator<IScriptDataValue> GetEnumerator() => ((IList<IScriptDataValue>)this.Value).GetEnumerator();
        public int IndexOf(IScriptDataValue item) => ((IList<IScriptDataValue>)this.Value).IndexOf(item);
        public void Insert(int index, IScriptDataValue item) => ((IList<IScriptDataValue>)this.Value).Insert(index, item);
        public bool Remove(IScriptDataValue item) => ((IList<IScriptDataValue>)this.Value).Remove(item);
        public void RemoveAt(int index) => ((IList<IScriptDataValue>)this.Value).RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => ((IList<IScriptDataValue>)this.Value).GetEnumerator();
        public static implicit operator List<IScriptDataValue>(ScriptDataStrictArray strictArray) => strictArray.Value;
        public static implicit operator ScriptDataStrictArray(List<IScriptDataValue> values) => new ScriptDataStrictArray { Value = values };
    }
}
