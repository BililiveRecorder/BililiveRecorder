using System;
using System.Runtime.Serialization;

namespace BililiveRecorder.Core.Api
{
    internal class BilibiliApiResponseCodeNotZeroException : Exception
    {
        public int? Code { get; }
        public string? Body { get; }

        public BilibiliApiResponseCodeNotZeroException(int? code, string? body) : base(message: "BiliBili API Code: " + (code?.ToString() ?? "(null)") + "\n" + body)
        {
            this.Code = code;
            this.Body = body;
        }

        public BilibiliApiResponseCodeNotZeroException() { }
        [Obsolete]
        public BilibiliApiResponseCodeNotZeroException(string message) : base(message) { }
        public BilibiliApiResponseCodeNotZeroException(string message, Exception innerException) : base(message, innerException) { }
        protected BilibiliApiResponseCodeNotZeroException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
