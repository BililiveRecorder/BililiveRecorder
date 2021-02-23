using System.IO;
using BililiveRecorder.Flv;
using Microsoft.IO;

namespace BililiveRecorder.Core
{
    public class RecyclableMemoryStreamProvider : IMemoryStreamProvider
    {
        private readonly RecyclableMemoryStreamManager manager = new RecyclableMemoryStreamManager(32 * 1024, 64 * 1024, 64 * 1024 * 32)
        {
            MaximumFreeSmallPoolBytes = 64 * 1024 * 1024,
            MaximumFreeLargePoolBytes = 64 * 1024 * 32,
        };

        public RecyclableMemoryStreamProvider()
        {
            //manager.StreamFinalized += () =>
            //{
            //    Debug.WriteLine("TestRecyclableMemoryStreamProvider: Stream Finalized");
            //};
            //manager.StreamDisposed += () =>
            //{
            //    // Debug.WriteLine("TestRecyclableMemoryStreamProvider: Stream Disposed");
            //};
        }

        public Stream CreateMemoryStream(string tag) => this.manager.GetStream(tag);
    }
}
