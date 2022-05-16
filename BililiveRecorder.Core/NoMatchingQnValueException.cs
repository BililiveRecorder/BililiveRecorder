using System;
using System.Runtime.Serialization;

namespace BililiveRecorder.Core
{
    internal class NoMatchingQnValueException : Exception
    {
        public NoMatchingQnValueException() { }
        public NoMatchingQnValueException(string message) : base(message) { }
        public NoMatchingQnValueException(string message, Exception innerException) : base(message, innerException) { }
        protected NoMatchingQnValueException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
