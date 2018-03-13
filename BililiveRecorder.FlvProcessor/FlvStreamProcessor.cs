using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvStreamProcessor : IDisposable
    {
        public RecordInfo Info; // not used for now.
        public readonly FlvHeader Header = new FlvHeader();
        private readonly List<FlvDataBlock> dataBlocks = new List<FlvDataBlock>();


        public event BlockProcessedEvent BlockProcessed;


        public FlvStreamProcessor(RecordInfo info)
        {
            Info = info;
        }



        public FlvClipProcessor Clip()
        {
            throw new NotImplementedException();
        }





        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FlvProcessor() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
