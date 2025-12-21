using System;
using Newtonsoft.Json;

namespace BililiveRecorder.Web.Models.Rest.Logs
{
    internal sealed class RawJsonStringConverter : JsonConverter<string>
    {
        public override string? ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer) => (string?)reader.Value;

        public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer) => writer.WriteRawValue(value);
    }
}
