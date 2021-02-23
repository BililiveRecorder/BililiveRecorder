using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BililiveRecorder.Core.SimpleWebhook
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EventType
    {
        Unknown,
        SessionStarted,
        SessionEnded,
        FileOpening,
        FileClosed,
    }
}
