using System.Diagnostics;
using System.IO;
using Microsoft.IO;

namespace BililiveRecorder.Flv.UnitTests
{
    public class TestRecyclableMemoryStreamProvider : IMemoryStreamProvider
    {
        private static readonly RecyclableMemoryStreamManager manager
            = new RecyclableMemoryStreamManager(32 * 1024, 64 * 1024, 64 * 1024 * 32)
            {
                MaximumFreeSmallPoolBytes = 64 * 1024 * 1024,
                MaximumFreeLargePoolBytes = 64 * 1024 * 32,
            };

        static TestRecyclableMemoryStreamProvider()
        {
            manager.StreamFinalized += () =>
            {
                Debug.WriteLine("TestRecyclableMemoryStreamProvider: Stream Finalized");
            };
            manager.StreamDisposed += () =>
            {
                // Debug.WriteLine("TestRecyclableMemoryStreamProvider: Stream Disposed");
            };
        }

        public Stream CreateMemoryStream(string tag) => manager.GetStream(tag);
    }
}
