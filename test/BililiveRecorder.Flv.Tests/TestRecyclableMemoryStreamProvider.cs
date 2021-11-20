using System.IO;
using Microsoft.IO;

namespace BililiveRecorder.Flv.Tests
{
    public class TestRecyclableMemoryStreamProvider : IMemoryStreamProvider
    {
        private static readonly RecyclableMemoryStreamManager manager
            = new RecyclableMemoryStreamManager(32 * 1024, 64 * 1024, 64 * 1024 * 32)
            {
                MaximumFreeSmallPoolBytes = 64 * 1024 * 1024,
                MaximumFreeLargePoolBytes = 64 * 1024 * 32,
            };

        public MemoryStream CreateMemoryStream(string tag) => manager.GetStream(tag);
    }
}
