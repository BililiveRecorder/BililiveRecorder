using System;
using System.Runtime.Serialization;

namespace BililiveRecorder.Flv.Parser
{
    public class NotFlvFileException : FlvException
    {
        public NotFlvFileException() { }

        public NotFlvFileException(string message) : base(message) { }

        public NotFlvFileException(string message, Exception innerException) : base(message, innerException) { }

        protected NotFlvFileException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
