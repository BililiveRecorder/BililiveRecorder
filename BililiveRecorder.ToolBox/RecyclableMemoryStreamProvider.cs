using System.IO;
using BililiveRecorder.Flv;
using Microsoft.IO;

namespace BililiveRecorder.ToolBox
{
    internal class RecyclableMemoryStreamProvider : IMemoryStreamProvider
    {
        private readonly RecyclableMemoryStreamManager manager;

        public RecyclableMemoryStreamProvider()
        {
            const int K = 1024;
            const int M = K * K;
            this.manager = new RecyclableMemoryStreamManager(new RecyclableMemoryStreamManager.Options
            {
                BlockSize = 32 * K,
                LargeBufferMultiple = 64 * K,
                MaximumBufferSize = 64 * K * 32,
                MaximumSmallPoolFreeBytes = 32 * M,
                MaximumLargePoolFreeBytes = 64 * K * 32,
            });

            //manager.StreamFinalized += () =>
            //{
            //    Debug.WriteLine("TestRecyclableMemoryStreamProvider: Stream Finalized");
            //};
            //manager.StreamDisposed += () =>
            //{
            //    // Debug.WriteLine("TestRecyclableMemoryStreamProvider: Stream Disposed");
            //};
        }

        public MemoryStream CreateMemoryStream(string tag) => this.manager.GetStream(tag);
    }
}
