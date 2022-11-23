using System;
using System.Runtime.Serialization;

namespace BililiveRecorder.Flv.Parser
{
    public class FlvException : Exception
    {
        /// <inheritdoc/>
        public FlvException() { }
        /// <inheritdoc/>
        public FlvException(string message) : base(message) { }
        /// <inheritdoc/>
        public FlvException(string message, Exception innerException) : base(message, innerException) { }
        /// <inheritdoc/>
        protected FlvException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class UnknownFlvTagTypeException : FlvException
    {
        public UnknownFlvTagTypeException() { }
        public UnknownFlvTagTypeException(string message) : base(message) { }
        public UnknownFlvTagTypeException(string message, Exception innerException) : base(message, innerException) { }
        protected UnknownFlvTagTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class NotFlvFileException : FlvException
    {
        public NotFlvFileException() { }
        public NotFlvFileException(string message) : base(message) { }
        public NotFlvFileException(string message, Exception innerException) : base(message, innerException) { }
        protected NotFlvFileException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    public class UnsupportedCodecException : FlvException
    {
        public UnsupportedCodecException() { }
        public UnsupportedCodecException(string message) : base(message) { }
        public UnsupportedCodecException(string message, Exception innerException) : base(message, innerException) { }
        protected UnsupportedCodecException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
