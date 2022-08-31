using System;
using Newtonsoft.Json;

namespace BililiveRecorder.Web.Models.Rest.Logs
{
    public class JsonLog : IComparable<JsonLog>
    {
        public long Id { get; set; }

        [JsonConverter(typeof(RawJsonStringConverter))]
        public string Log { get; set; } = "{}";

        int IComparable<JsonLog>.CompareTo(JsonLog? other) => this.Id.CompareTo(other?.Id);
    }
}
