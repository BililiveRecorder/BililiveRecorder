using System;
using System.Runtime.Serialization;

namespace BililiveRecorder.Flv.Amf
{
    public class AmfException : Exception
    {
        /// <inheritdoc/>
        public AmfException() { }
        /// <inheritdoc/>
        public AmfException(string message) : base(message) { }
        /// <inheritdoc/>
        public AmfException(string message, Exception innerException) : base(message, innerException) { }
        /// <inheritdoc/>
        protected AmfException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
