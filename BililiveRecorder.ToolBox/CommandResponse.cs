using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BililiveRecorder.ToolBox
{
    public class CommandResponse<TResponse> where TResponse : class
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ResponseStatus Status { get; set; }

        public TResponse? Result { get; set; }

        public string? ErrorMessage { get; set; }

        public Exception? Exception { get; set; }
    }
}
