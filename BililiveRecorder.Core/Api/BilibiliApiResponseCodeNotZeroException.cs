using System;
using System.Runtime.Serialization;

namespace BililiveRecorder.Core.Api
{
    public class BilibiliApiResponseCodeNotZeroException : Exception
    {
        public BilibiliApiResponseCodeNotZeroException() { }
        public BilibiliApiResponseCodeNotZeroException(string message) : base(message) { }
        public BilibiliApiResponseCodeNotZeroException(string message, Exception innerException) : base(message, innerException) { }
        protected BilibiliApiResponseCodeNotZeroException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
