using System.Collections.Generic;
using System.Linq;

namespace BililiveRecorder.Core.Scripting.Runtime
{
    internal class JintStorage
    {
        private readonly Dictionary<string, string> storage = new();

        public string? GetItem(string key) => this.storage.TryGetValue(key, out var value) ? value : null;
        public void SetItem(string key, string value) => this.storage[key] = value;
        public void RemoveItem(string key) => this.storage.Remove(key);
        public void Clear() => this.storage.Clear();
        public string? Key(int index) => this.storage.Count > index ? this.storage.Keys.ElementAt(index) : null;
        public int Length => this.storage.Count;
    }
}
