using System.IO;
using Microsoft.IO;

namespace BililiveRecorder.Flv.Tests
{
    public class TestRecyclableMemoryStreamProvider : IMemoryStreamProvider
    {
        private static readonly RecyclableMemoryStreamManager manager
            = new RecyclableMemoryStreamManager(new RecyclableMemoryStreamManager.Options
            {
                BlockSize = 32 * 1024,
                LargeBufferMultiple = 64 * 1024,
                MaximumBufferSize = 64 * 1024 * 32,
                MaximumSmallPoolFreeBytes = 64 * 1024 * 1024,
                MaximumLargePoolFreeBytes = 64 * 1024 * 32,
            });

        public MemoryStream CreateMemoryStream(string tag) => manager.GetStream(tag);
    }
}
