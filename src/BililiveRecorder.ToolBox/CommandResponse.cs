using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BililiveRecorder.ToolBox
{
    public class CommandResponse<TResponseData> where TResponseData : IResponseData
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ResponseStatus Status { get; set; }

        public TResponseData? Data { get; set; }

        public string? ErrorMessage { get; set; }

        public Exception? Exception { get; set; }
    }
}
