using JsonSubTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BililiveRecorder.Core.Config
{
    [JsonConverter(typeof(JsonSubtypes), nameof(Version))]
    [JsonSubtypes.KnownSubType(typeof(V1.ConfigV1Wrapper), 1)]
    [JsonSubtypes.KnownSubType(typeof(V2.ConfigV2), 2)]
    public abstract class ConfigBase
    {
        [JsonProperty("version")]
        public virtual int Version { get; internal protected set; }
    }
}
