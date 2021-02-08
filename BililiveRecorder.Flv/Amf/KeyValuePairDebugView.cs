using System.Diagnostics;

namespace BililiveRecorder.Flv.Amf
{
    [DebuggerDisplay("{Key}: {Value}")]
    internal sealed class KeyValuePairDebugView<K, V>
    {
        public KeyValuePairDebugView(K key, V value)
        {
            this.Key = key;
            this.Value = value;
        }

        public K Key { get; }
        public V Value { get; }
    }
}
