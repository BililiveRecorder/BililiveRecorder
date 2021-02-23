using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Xml
{
    /// <summary>
    /// 从 <see cref="IReadOnlyList{}"/> 读取 Flv Tag
    /// </summary>
    /// <remarks>
    /// 主要在调试修复算法和测试时使用
    /// </remarks>
    public class FlvTagListReader : IFlvTagReader
    {
        private readonly IReadOnlyList<Tag> tags;
        private int index;

        public FlvTagListReader(IReadOnlyList<Tag> tags)
        {
            this.tags = tags ?? throw new ArgumentNullException(nameof(tags));
        }

        public Task<Tag?> PeekTagAsync(System.Threading.CancellationToken token) => Task.FromResult(this.index < this.tags.Count ? this.tags[this.index] : null)!;

        public Task<Tag?> ReadTagAsync(System.Threading.CancellationToken token) => Task.FromResult(this.index < this.tags.Count ? this.tags[this.index++] : null);

        public void Dispose() { }
    }
}
