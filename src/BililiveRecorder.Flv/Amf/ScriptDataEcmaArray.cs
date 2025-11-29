using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace BililiveRecorder.Flv.Amf
{
    [DebuggerTypeProxy(typeof(AmfDictionaryDebugView))]
    [DebuggerDisplay("AmfEcmaArray, Count = {Count}")]
    public class ScriptDataEcmaArray : IScriptDataValue, IDictionary<string, IScriptDataValue>, ICollection<KeyValuePair<string, IScriptDataValue>>, IEnumerable<KeyValuePair<string, IScriptDataValue>>, IReadOnlyCollection<KeyValuePair<string, IScriptDataValue>>, IReadOnlyDictionary<string, IScriptDataValue>
    {
        public ScriptDataType Type => ScriptDataType.EcmaArray;

        [JsonProperty]
        public Dictionary<string, IScriptDataValue> Value { get; set; } = new Dictionary<string, IScriptDataValue>();

        public void WriteTo(Stream stream)
        {
            stream.WriteByte((byte)this.Type);

            {
                var buffer = new byte[sizeof(uint)];
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)this.Value.Count);
                stream.Write(buffer);
            }

            foreach (var item in this.Value)
            {
                // key
                var bytes = Encoding.UTF8.GetBytes(item.Key);
                if (bytes.Length > ushort.MaxValue)
                    throw new AmfException($"Cannot write more than {ushort.MaxValue} into ScriptDataString");

                var buffer = new byte[sizeof(ushort)];
                BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)bytes.Length);

                stream.Write(buffer);
                stream.Write(bytes);

                // value
                item.Value.WriteTo(stream);
            }

            stream.Write(new byte[] { 0, 0, 9 });
        }

        public IScriptDataValue this[string key] { get => ((IDictionary<string, IScriptDataValue>)this.Value)[key]; set => ((IDictionary<string, IScriptDataValue>)this.Value)[key] = value; }
        public ICollection<string> Keys => ((IDictionary<string, IScriptDataValue>)this.Value).Keys;
        public ICollection<IScriptDataValue> Values => ((IDictionary<string, IScriptDataValue>)this.Value).Values;
        IEnumerable<string> IReadOnlyDictionary<string, IScriptDataValue>.Keys => ((IReadOnlyDictionary<string, IScriptDataValue>)this.Value).Keys;
        IEnumerable<IScriptDataValue> IReadOnlyDictionary<string, IScriptDataValue>.Values => ((IReadOnlyDictionary<string, IScriptDataValue>)this.Value).Values;
        public int Count => ((IDictionary<string, IScriptDataValue>)this.Value).Count;
        public bool IsReadOnly => ((IDictionary<string, IScriptDataValue>)this.Value).IsReadOnly;
        public void Add(string key, IScriptDataValue value) => ((IDictionary<string, IScriptDataValue>)this.Value).Add(key, value);
        public void Add(KeyValuePair<string, IScriptDataValue> item) => ((IDictionary<string, IScriptDataValue>)this.Value).Add(item);
        public void Clear() => ((IDictionary<string, IScriptDataValue>)this.Value).Clear();
        public bool Contains(KeyValuePair<string, IScriptDataValue> item) => ((IDictionary<string, IScriptDataValue>)this.Value).Contains(item);
        public bool ContainsKey(string key) => ((IDictionary<string, IScriptDataValue>)this.Value).ContainsKey(key);
        public void CopyTo(KeyValuePair<string, IScriptDataValue>[] array, int arrayIndex) => ((IDictionary<string, IScriptDataValue>)this.Value).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, IScriptDataValue>> GetEnumerator() => ((IDictionary<string, IScriptDataValue>)this.Value).GetEnumerator();
        public bool Remove(string key) => ((IDictionary<string, IScriptDataValue>)this.Value).Remove(key);
        public bool Remove(KeyValuePair<string, IScriptDataValue> item) => ((IDictionary<string, IScriptDataValue>)this.Value).Remove(item);
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out IScriptDataValue value) => ((IDictionary<string, IScriptDataValue>)this.Value).TryGetValue(key, out value!);
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        IEnumerator IEnumerable.GetEnumerator() => ((IDictionary<string, IScriptDataValue>)this.Value).GetEnumerator();
        public static implicit operator Dictionary<string, IScriptDataValue>(ScriptDataEcmaArray ecmaArray) => ecmaArray.Value;
        public static implicit operator ScriptDataEcmaArray(Dictionary<string, IScriptDataValue> ecmaArray) => new ScriptDataEcmaArray { Value = ecmaArray };
        public static implicit operator ScriptDataEcmaArray(ScriptDataObject @object) => new ScriptDataEcmaArray { Value = @object };
    }
}
