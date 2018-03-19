using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvClipProcessor : IDisposable
    {
        public readonly FlvMetadata Header;
        public List<FlvTag> Tags;
        private int target = -1;

        public Func<string> GetFileName;

        public FlvClipProcessor(FlvMetadata header, List<FlvTag> past, int future)
        {
            Header = header;
            Tags = past;
            target = Tags[Tags.Count - 1].TimeStamp + (future * FlvStreamProcessor.SEC_TO_MS);
        }

        public void AddTag(FlvTag tag)
        {
            Tags.Add(tag);
            if (tag.TimeStamp >= target)
            {
                FinallizeFile();
            }
        }

        public void FinallizeFile()
        {
            using (var fs = new FileStream(GetFileName(), FileMode.CreateNew, FileAccess.ReadWrite))
            {
                fs.Write(FlvStreamProcessor.FLV_HEADER_BYTES, 0, FlvStreamProcessor.FLV_HEADER_BYTES.Length);

                Header.Meta["duration"] = Tags[Tags.Count - 1].TimeStamp / 1000.0;
                Header.Meta["lasttimestamp"] = (double)Tags[Tags.Count - 1].TimeStamp;

                var t = new FlvTag();
                t.TagType = TagType.DATA;
                t.Data = Header.ToBytes();
                var b = t.ToBytes();
                fs.Write(b, 0, b.Length);
                fs.Write(t.Data, 0, t.Data.Length);
                fs.Write(BitConverter.GetBytes(t.Data.Length + b.Length).ToBE(), 0, 4);

                int timestamp = Tags[0].TimeStamp;

                Tags.ForEach(tag =>
                {
                    tag.TimeStamp -= timestamp;
                    var vs = tag.ToBytes();
                    fs.Write(vs, 0, vs.Length);
                    fs.Write(tag.Data, 0, tag.Data.Length);
                    fs.Write(BitConverter.GetBytes(tag.Data.Length + vs.Length).ToBE(), 0, 4);
                });

                fs.Close();
            }

            ClipFinalized?.Invoke(this, new ClipFinalizedArgs() { ClipProcessor = this });
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
