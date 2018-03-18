using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvStreamProcessor : IDisposable
    {
        private const int MIN_BUFFER_SIZE = 1024 * 2;

        public RecordInfo Info; // not used for now.
        public readonly FlvMetadata Metadata = new FlvMetadata();
        public event TagProcessedEvent TagProcessed;

        private byte[] headers;

        private bool _headerParsed = false;
        private readonly List<FlvTag> Tags = new List<FlvTag>();
        private readonly MemoryStream _buffer = new MemoryStream();
        private readonly MemoryStream _data = new MemoryStream();
        private FlvTag currentTag = null;

        public FlvStreamProcessor(RecordInfo info)
        {
            Info = info;
        }

        public void AddBytes(byte[] data)
        {
            // lock ( ) { _AddBytes() }
        }

        private void _AddBytes(byte[] data)
        {
            if (currentTag == null)
            {
                if (_buffer.Position >= MIN_BUFFER_SIZE)
                {
                    _ParseTag(data);
                }
            }
            else
            {
                _WriteTagData(data);
            }
        }

        private void _WriteTagData(byte[] data)
        {
            int toRead = Math.Min(data.Length, (currentTag.TagSize - (int)_data.Position));
            _data.Write(data, 0, toRead);
            if ((int)_data.Position == currentTag.TagSize)
            {
                currentTag.Data = _data.ToArray();
                _data.SetLength(0); // reset data buffer
                _TagCreated(currentTag);
                currentTag = null;
                _AddBytes(data.Skip(toRead).ToArray());
            }
        }

        private void _TagCreated(FlvTag tag)
        {
            tag.TimeStamp -= 0;//TODO: 修复时间戳
            Tags.Add(tag);
            // TODO: remove old tag
            TagProcessed?.Invoke(this, new TagProcessedArgs() { Tag = tag });
            if (tag.TagType == TagType.DATA)
            {
                // TODO: onMetaData
            }
        }


        private void _ParseTag(byte[] data)
        {
            byte[] b = { 0, 0, 0, 0, };
            _buffer.Write(data, 0, data.Length);
            long dataLen = _buffer.Position;
            _buffer.Position = 0;
            FlvTag tag = new FlvTag();

            // TagType UI8
            tag.TagType = (TagType)_buffer.ReadByte();
            Debug.Write(string.Format("Tag Type: {0}\n", tag.TagType));

            // DataSize UI24
            _buffer.Read(b, 1, 3);
            tag.TagSize = BitConverter.ToInt32(b.ToBE(), 0); // TODO: test this

            // Timestamp UI24
            _buffer.Read(b, 1, 3);
            // TimestampExtended UI8
            _buffer.Read(b, 0, 1);
            tag.TimeStamp = BitConverter.ToInt32(b.ToBE(), 0);

            // StreamID UI24
            _buffer.Read(tag.StreamId, 0, 3);

            currentTag = tag;
            byte[] rest = _buffer.GetBuffer().Skip((int)_buffer.Position).Take((int)(dataLen - _buffer.Position)).ToArray();
            _buffer.Position = 0;

            _AddBytes(rest);
        }

        private void _ParseHeader()
        {

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
