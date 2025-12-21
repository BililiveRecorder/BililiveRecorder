using JsonSubTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BililiveRecorder.Core.Config
{
#pragma warning disable CS0618 // Type or member is obsolete
    [JsonConverter(typeof(JsonSubtypes), nameof(Version))]
    [JsonSubtypes.KnownSubType(typeof(V1.ConfigV1Wrapper), 1)]
    [JsonSubtypes.KnownSubType(typeof(V2.ConfigV2), 2)]
    [JsonSubtypes.KnownSubType(typeof(V3.ConfigV3), 3)]
#pragma warning restore CS0618 // Type or member is obsolete
    public abstract class ConfigBase
    {
        [JsonProperty("$schema", Order = -2)]
        public string? DollarSignSchema { get; set; }

        [JsonProperty("version")]
        public virtual int Version { get; internal protected set; }
    }
}
