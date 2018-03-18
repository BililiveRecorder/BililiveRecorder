using System;
using System.Collections.Generic;
using System.Text;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvClipProcessor : IDisposable
    {
        public readonly FlvMetadata Header;

        public FlvClipProcessor(FlvMetadata header)
        {
            Header = header;
        }

        public void AddTag(FlvTag tag)
        {
            throw new NotImplementedException();
        }


        public event ClipFinalizedEvent ClipFinalized;

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
        // ~FlvClipProcessor() {
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
