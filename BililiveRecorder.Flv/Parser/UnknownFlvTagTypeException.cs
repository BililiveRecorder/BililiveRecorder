using System;
using System.Runtime.Serialization;

namespace BililiveRecorder.Flv.Parser
{
    public class UnknownFlvTagTypeException : FlvException
    {
        public UnknownFlvTagTypeException() { }

        public UnknownFlvTagTypeException(string message) : base(message) { }

        public UnknownFlvTagTypeException(string message, Exception innerException) : base(message, innerException) { }

        protected UnknownFlvTagTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
