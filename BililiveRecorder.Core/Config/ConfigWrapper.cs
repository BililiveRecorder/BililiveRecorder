using Newtonsoft.Json;

namespace BililiveRecorder.Core.Config
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class ConfigWrapper
    {
        /// <summary>
        /// Config Version
        /// </summary>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>
        /// Config Data String
        /// </summary>
        [JsonProperty("data")]
        public string Data { get; set; }
    }
}
