using System;
using System.Runtime.Serialization;

namespace BililiveRecorder.Core.Api
{
    internal class Http412Exception : Exception
    {
        public Http412Exception() { }
        public Http412Exception(string message) : base(message) { }
        public Http412Exception(string message, Exception innerException) : base(message, innerException) { }
        protected Http412Exception(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
