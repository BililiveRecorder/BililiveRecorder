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
}
