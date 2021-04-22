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
            this.manager = new RecyclableMemoryStreamManager(32 * K, 64 * K, 64 * K * 32)
            {
                MaximumFreeSmallPoolBytes = 32 * M,
                MaximumFreeLargePoolBytes = 64 * K * 32,
            };

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
